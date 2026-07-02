"""Long balance-focused playtest. Runs the sim headless for many days and reports market
health: return distribution, runaways/crashes/stuck stocks, news volume/severity, and any
instability (errors / NaN). Passive observer — places no trades."""
import asyncio, json, sys, math, statistics

PORT = int(sys.argv[1])
TARGET_DAYS = int(sys.argv[2]) if len(sys.argv) > 2 else 40

async def main():
    import websockets
    async with websockets.connect(f"ws://127.0.0.1:{PORT}", open_timeout=15, max_size=None) as ws:
        await ws.send(json.dumps({"type": "NewGame", "payload": {
            "playerName": "BalanceBot", "startingCash": 100000, "stockCount": 40, "seed": 123}}))
        await ws.send(json.dumps({"type": "SetSpeed", "payload": {"speed": 5}}))
        await ws.send(json.dumps({"type": "UpdateSettings", "payload": {"SkipWeekends": True}}))

        days, errors = set(), []
        first, last, sect = {}, {}, {}
        bad = []
        news_per_day, sev = {}, {}
        cur_day = None

        for _ in range(400000):
            try:
                data = json.loads(await asyncio.wait_for(ws.recv(), timeout=20))
            except asyncio.TimeoutError:
                print("TIMEOUT"); break
            t, p = data.get("type"), data.get("payload", {})

            if t == "MarketSnapshot":
                for s in p.get("stocks", []):
                    sym = s.get("symbol"); first.setdefault(sym, s.get("price", 0)); sect[sym] = s.get("sector")
            elif t == "MarketUpdate":
                d = (p.get("gameTime") or "")[:10]
                if d and d != cur_day: cur_day = d; days.add(d)
                for u in p.get("prices", []):
                    sym = u.get("Symbol", u.get("symbol")); pr = u.get("price", 0); last[sym] = pr
                    if pr is None or pr < 0 or (isinstance(pr, float) and (math.isnan(pr) or math.isinf(pr))): bad.append((sym, pr))
                if len(days) >= TARGET_DAYS: break
            elif t == "NewsEvents":
                for e in p.get("events", []):
                    news_per_day[cur_day] = news_per_day.get(cur_day, 0) + 1
                    s = e.get("severity", "?"); sev[s] = sev.get(s, 0) + 1
            elif t == "error":
                errors.append(p)

        rets = []
        runaway, crashed, stuck = [], [], []
        for sym, f in first.items():
            l = last.get(sym, f)
            if f and f > 0:
                r = (l - f) / f * 100
                rets.append(r)
                if r > 200: runaway.append((sym, round(r), sect.get(sym)))
                if r < -90: crashed.append((sym, round(r), sect.get(sym)))
                if abs(r) < 0.01: stuck.append(sym)

        npd = list(news_per_day.values())
        print("\n" + "=" * 60)
        print(f"BALANCE PLAYTEST — {len(days)} days, {len(first)} stocks")
        print("=" * 60)
        print(f"Errors: {len(errors)}   Bad prices (NaN/neg): {len(bad)} {bad[:3]}")
        if rets:
            rets.sort()
            print(f"\nReturns over period (%):")
            print(f"  min {rets[0]:.1f} | p25 {rets[len(rets)//4]:.1f} | median {statistics.median(rets):.1f} "
                  f"| p75 {rets[3*len(rets)//4]:.1f} | max {rets[-1]:.1f}")
            print(f"  mean {statistics.mean(rets):.1f} | stdev {statistics.pstdev(rets):.1f}")
        print(f"\nRunaways (>+200%): {len(runaway)} {runaway[:6]}")
        print(f"Crashes  (<-90%):  {len(crashed)} {crashed[:6]}")
        print(f"Stuck (no move):   {len(stuck)} {stuck[:6]}")
        if npd:
            print(f"\nNews/day: min {min(npd)} | median {int(statistics.median(npd))} | max {max(npd)} | total {sum(npd)}")
        print(f"Severity mix: {sev}")
        print("\nBALANCE OK" if not errors and not bad else "\nISSUES FOUND")

asyncio.run(main())
