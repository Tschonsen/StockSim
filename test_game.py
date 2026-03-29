"""StockSim Playtest — Extended runtime test via WebSocket."""
import asyncio, websockets, json, time

async def test():
    uri = "ws://localhost:8765"
    async with websockets.connect(uri, open_timeout=10) as ws:
        # Start new game with VeryFast speed for quick simulation
        await ws.send(json.dumps({"type": "NewGame", "payload": {
            "playerName": "PlaytestBot", "startingCash": 100000, "stockCount": 30, "seed": 42
        }}))
        await ws.send(json.dumps({"type": "SetSpeed", "payload": {"speed": 5}}))  # Maximum
        await ws.send(json.dumps({"type": "UpdateSettings", "payload": {"SkipWeekends": True}}))

        tick_count = 0
        prices = {}          # symbol -> [price list]
        all_stocks = {}      # symbol -> {sector, name}
        news = []
        news_unique = set()
        types_seen = {}
        market_open_ticks = 0
        market_closed_ticks = 0
        day_changes = []
        current_day = None
        days_seen = set()
        errors = []
        dividends = []
        earnings = []
        start_time = time.time()
        target_ticks = 2000  # ~5 game days with SkipWeekends

        for _ in range(15000):  # More iterations for high-speed playtest
            try:
                raw = await asyncio.wait_for(ws.recv(), timeout=15)
                data = json.loads(raw)
                t = data.get("type", "?")
                p = data.get("payload", {})
                types_seen[t] = types_seen.get(t, 0) + 1

                if t == "MarketSnapshot":
                    stocks = p.get("stocks", [])
                    for s in stocks:
                        sym = s.get("symbol", "?")
                        all_stocks[sym] = {"sector": s.get("sector", "?"), "name": s.get("name", "?")}
                        prices[sym] = [s.get("price", 0)]
                    print(f"MarketSnapshot: {len(stocks)} stocks")

                elif t == "MarketUpdate":
                    tick_count += 1
                    updates = p.get("prices", [])
                    gt = p.get("gameTime", "")
                    mo = p.get("isMarketOpen", False)
                    tick_num = p.get("tick", 0)

                    if mo:
                        market_open_ticks += 1
                    else:
                        market_closed_ticks += 1

                    # Track day changes
                    day = gt[:10] if gt else "?"
                    if day != current_day:
                        if current_day:
                            day_changes.append(current_day)
                        current_day = day
                        days_seen.add(day)

                    for u in updates:
                        sym = u.get("Symbol", u.get("symbol", "?"))
                        pr = u.get("price", 0)
                        if sym not in prices:
                            prices[sym] = []
                        prices[sym].append(pr)

                    if tick_count <= 3 or tick_count % 200 == 0:
                        print(f"T{tick_count} (tick={tick_num}): {gt[:19]} open={mo} updates={len(updates)}")

                    if tick_count >= target_ticks:
                        break

                elif t == "NewsEvents":
                    for e in p.get("events", []):
                        h = e.get("headline", "")
                        news.append(h)
                        news_unique.add(h)

                elif t == "DividendPaid":
                    dividends.append(p)

                elif t == "SpeedChanged":
                    print(f"Speed: {p.get('speed')}")

                elif t == "error":
                    errors.append(p)
                    print(f"ERROR: {p}")

            except asyncio.TimeoutError:
                print(f"Timeout at tick {tick_count}")
                break

        elapsed = time.time() - start_time

        # === COMPREHENSIVE RESULTS ===
        print(f"\n{'='*70}")
        print(f"PLAYTEST RESULTS")
        print(f"{'='*70}")
        print(f"Duration:         {elapsed:.1f}s real time")
        print(f"Ticks:            {tick_count} ({market_open_ticks} market open, {market_closed_ticks} closed)")
        print(f"Days simulated:   {len(days_seen)} ({', '.join(sorted(days_seen)[:5])}{'...' if len(days_seen) > 5 else ''})")
        print(f"Stocks:           {len(all_stocks)}")
        print(f"Message types:    {types_seen}")
        print(f"Errors:           {len(errors)}")

        # Price analysis
        print(f"\n--- PRICE ANALYSIS ---")
        stuck_stocks = []
        volatile_stocks = []
        penny_stocks = []
        for sym, pp in sorted(prices.items()):
            if len(pp) < 2:
                continue
            unique = len(set(round(x, 6) for x in pp))
            start_p = pp[0]
            end_p = pp[-1]
            min_p = min(pp)
            max_p = max(pp)
            ch = ((end_p - start_p) / start_p * 100) if start_p > 0 else 0
            spread = ((max_p - min_p) / start_p * 100) if start_p > 0 else 0
            info = all_stocks.get(sym, {})

            if unique <= 1:
                stuck_stocks.append(sym)
            if abs(ch) > 20:
                volatile_stocks.append((sym, ch))
            if end_p < 1.0:
                penny_stocks.append((sym, end_p))

        # Top movers
        movers = []
        for sym, pp in prices.items():
            if len(pp) > 1 and pp[0] > 0:
                ch = ((pp[-1] - pp[0]) / pp[0] * 100)
                movers.append((sym, pp[0], pp[-1], ch, all_stocks.get(sym, {}).get("sector", "?")))
        movers.sort(key=lambda x: x[3], reverse=True)

        print(f"Top 5 Gainers:")
        for sym, sp, ep, ch, sec in movers[:5]:
            print(f"  {sym:8s} ({sec:12s}): ${sp:8.2f} -> ${ep:8.2f} ({ch:+.2f}%)")
        print(f"Top 5 Losers:")
        for sym, sp, ep, ch, sec in movers[-5:]:
            print(f"  {sym:8s} ({sec:12s}): ${sp:8.2f} -> ${ep:8.2f} ({ch:+.2f}%)")

        print(f"\nStuck (no movement):  {len(stuck_stocks)} {stuck_stocks[:5]}")
        print(f"Extreme moves (>20%): {len(volatile_stocks)} {[(s,f'{c:.1f}%') for s,c in volatile_stocks[:5]]}")
        print(f"Penny stocks (<$1):   {len(penny_stocks)} {[(s,f'${p:.2f}') for s,p in penny_stocks[:5]]}")

        # Sector performance
        print(f"\n--- SECTOR PERFORMANCE ---")
        sector_changes = {}
        for sym, pp in prices.items():
            if len(pp) < 2 or pp[0] == 0:
                continue
            sec = all_stocks.get(sym, {}).get("sector", "?")
            ch = ((pp[-1] - pp[0]) / pp[0] * 100)
            if sec not in sector_changes:
                sector_changes[sec] = []
            sector_changes[sec].append(ch)
        for sec, changes in sorted(sector_changes.items()):
            avg = sum(changes) / len(changes)
            print(f"  {sec:15s}: avg {avg:+.2f}% ({len(changes)} stocks)")

        # News analysis
        print(f"\n--- NEWS ANALYSIS ---")
        print(f"Total news sent:  {len(news)}")
        print(f"Unique headlines: {len(news_unique)}")
        dup_ratio = (len(news) - len(news_unique)) / max(len(news), 1) * 100
        print(f"Duplicate ratio:  {dup_ratio:.1f}%")
        print(f"Sample headlines:")
        for h in list(news_unique)[:10]:
            print(f"  - {h[:120]}")

        # Dividends
        print(f"\n--- DIVIDENDS ---")
        print(f"Dividend payments: {len(dividends)}")
        for d in dividends[:5]:
            print(f"  {d.get('symbol','?')}: ${d.get('net',0):.2f} net ({d.get('shares',0)} shares)")

        # Session 25 findings verification
        print(f"\n--- SESSION 25 FINDINGS CHECK ---")
        trading_days = max(len(days_seen) - 1, 1)  # approximate trading days
        news_per_day = len(news) / trading_days if trading_days > 0 else 0
        extreme_pct = len(volatile_stocks) / max(len(all_stocks), 1) * 100
        worst_sector = min((avg for _, changes in sector_changes.items() for avg in [sum(changes)/len(changes)]), default=0)
        best_sector = max((avg for _, changes in sector_changes.items() for avg in [sum(changes)/len(changes)]), default=0)
        worst_stock = movers[-1][3] if movers else 0

        print(f"  Finding 1 (Sector Drift):  worst={worst_sector:+.1f}%, best={best_sector:+.1f}% (target: <±15%)")
        print(f"  Finding 2 (>20% moves):    {len(volatile_stocks)}/{len(all_stocks)} = {extreme_pct:.0f}% (target: <5%)")
        print(f"  Finding 3 (News/day):      {news_per_day:.1f} (target: 3-5)")
        print(f"  Finding 4 (Dividends):     {len(dividends)} payments")
        print(f"  Finding 5 (Worst loser):   {worst_stock:+.1f}% (target: >-20%)")

        # Validation
        print(f"\n--- VALIDATION ---")
        issues = []
        if stuck_stocks:
            issues.append(f"WARN: {len(stuck_stocks)} stocks stuck")
        if len(errors) > 0:
            issues.append(f"FAIL: {len(errors)} errors")
        if tick_count < target_ticks * 0.8:
            issues.append(f"WARN: Only {tick_count}/{target_ticks} ticks reached")
        if dup_ratio > 10:
            issues.append(f"FAIL: {dup_ratio:.0f}% duplicate news")
        if len(news_unique) < 3:
            issues.append(f"WARN: Very few unique news ({len(news_unique)})")
        if len(days_seen) < 2:
            issues.append(f"WARN: Only {len(days_seen)} days simulated")
        if extreme_pct > 5:
            issues.append(f"WARN: {extreme_pct:.0f}% stocks with >20% move (target: <5%)")
        if news_per_day < 3:
            issues.append(f"WARN: Only {news_per_day:.1f} news/day (target: 3-5)")

        for i in issues:
            print(f"  {i}")
        if not issues:
            print("  All checks passed!")

        print(f"\n{'PASSED' if len([i for i in issues if 'FAIL' in i]) == 0 else 'FAILED'}")

asyncio.run(test())
