"""StockSim Options Playtest — Tests options chain, pricing, and trading via WebSocket."""
import asyncio, websockets, json, time

async def test():
    uri = "ws://localhost:8765"
    async with websockets.connect(uri, open_timeout=10) as ws:
        # Start new game
        await ws.send(json.dumps({"type": "NewGame", "payload": {
            "playerName": "OptionsBot", "startingCash": 100000, "stockCount": 30, "seed": 42
        }}))
        await ws.send(json.dumps({"type": "SetSpeed", "payload": {"speed": 3}}))  # Fast
        await ws.send(json.dumps({"type": "UpdateSettings", "payload": {"SkipWeekends": True}}))

        stocks = []
        options_chain = None
        buy_result = None
        news = []
        errors = []
        tick_count = 0

        # Phase 1: Get initial data
        for _ in range(200):
            raw = await asyncio.wait_for(ws.recv(), timeout=15)
            data = json.loads(raw)
            t = data.get("type", "?")
            p = data.get("payload", {})

            if t == "MarketSnapshot":
                stocks = [s.get("symbol") for s in p.get("stocks", [])]
                print(f"MarketSnapshot: {len(stocks)} stocks")

            elif t == "MarketUpdate":
                tick_count += 1
                if tick_count == 5:
                    # After a few ticks, request options chain for first stock
                    test_symbol = stocks[0] if stocks else "GRDT"
                    print(f"Requesting options chain for {test_symbol}...")
                    await ws.send(json.dumps({"type": "GetOptionsChain", "payload": {"Symbol": test_symbol}}))

            elif t == "OptionsChain":
                options_chain = p
                symbol = p.get("symbol", "?")
                slices = p.get("slices", [])
                no_chain = p.get("noChain", False)

                if no_chain:
                    print(f"No options chain for {symbol} (market cap too low)")
                    # Try a different stock
                    for s in stocks[1:10]:
                        await ws.send(json.dumps({"type": "GetOptionsChain", "payload": {"Symbol": s}}))
                        break
                else:
                    print(f"\nOptions Chain: {symbol}")
                    print(f"  Expirations: {len(slices)}")
                    if slices:
                        s = slices[0]
                        print(f"  First expiry: {s.get('expirationDate', '?')[:10]} ({s.get('daysToExpiry')}d)")
                        calls = s.get("calls", [])
                        puts = s.get("puts", [])
                        print(f"  Strikes: {len(s.get('strikes', []))}")
                        print(f"  Calls: {len(calls)}, Puts: {len(puts)}")

                        # Print a few ATM options
                        if calls:
                            mid = len(calls) // 2
                            for i in range(max(0, mid-2), min(len(calls), mid+3)):
                                c = calls[i]
                                print(f"    Call ${c['strike']:>7.0f} | Bid {c['bid']:>6.2f} Ask {c['ask']:>6.2f} | IV {c['iv']:>5.1f}% | Delta {c['delta']:>6.3f} | Theta {c['theta']:>7.4f} | Vol {c['volume']:>5}")

                        if puts:
                            mid = len(puts) // 2
                            for i in range(max(0, mid-2), min(len(puts), mid+3)):
                                p2 = puts[i]
                                print(f"    Put  ${p2['strike']:>7.0f} | Bid {p2['bid']:>6.2f} Ask {p2['ask']:>6.2f} | IV {p2['iv']:>5.1f}% | Delta {p2['delta']:>7.3f} | Theta {p2['theta']:>7.4f} | Vol {p2['volume']:>5}")

                        # Try buying an ATM call
                        if calls:
                            atm_call = calls[len(calls)//2]
                            print(f"\n  Buying 1x Call ${atm_call['strike']:.0f} @ ${atm_call['ask']:.2f}...")
                            await ws.send(json.dumps({"type": "BuyOption", "payload": {
                                "ContractId": atm_call["id"],
                                "Symbol": symbol,
                                "Quantity": 1
                            }}))

            elif t == "OptionOrderResult":
                buy_result = p
                success = p.get("success", False)
                msg = p.get("message", "?")
                print(f"  Order Result: {'SUCCESS' if success else 'FAILED'} — {msg}")
                if success:
                    print(f"  Price: ${p.get('price', 0):.2f}, Qty: {p.get('quantity', 0)}")

            elif t == "NewsEvents":
                for e in p.get("events", []):
                    h = e.get("headline", "")
                    if "option" in h.lower() or "iv" in h.lower() or "pin" in h.lower() or "unusual" in h.lower():
                        news.append(h)
                        print(f"  OPTIONS NEWS: {h}")

            elif t == "error":
                errors.append(p)
                print(f"ERROR: {p}")

            if tick_count >= 100 and options_chain and not options_chain.get("noChain"):
                break

        # === RESULTS ===
        print(f"\n{'='*60}")
        print(f"OPTIONS PLAYTEST RESULTS")
        print(f"{'='*60}")
        print(f"Ticks:          {tick_count}")
        print(f"Stocks:         {len(stocks)}")
        print(f"Chain received: {'Yes' if options_chain and not options_chain.get('noChain') else 'No'}")
        if options_chain and not options_chain.get("noChain"):
            slices = options_chain.get("slices", [])
            total_contracts = sum(len(s.get("calls",[])) + len(s.get("puts",[])) for s in slices)
            print(f"Expirations:    {len(slices)}")
            print(f"Total contracts:{total_contracts}")

            # Validate pricing
            issues = []
            for s in slices:
                for c in s.get("calls", []) + s.get("puts", []):
                    if c["theo"] <= 0: issues.append(f"Zero price: {c['type']} ${c['strike']}")
                    if c["bid"] >= c["ask"]: issues.append(f"Bid >= Ask: {c['type']} ${c['strike']}")
                    if c["iv"] <= 0: issues.append(f"Zero IV: {c['type']} ${c['strike']}")
                    if c["type"] == "Call" and (c["delta"] < 0 or c["delta"] > 1):
                        issues.append(f"Bad call delta {c['delta']}: ${c['strike']}")
                    if c["type"] == "Put" and (c["delta"] > 0 or c["delta"] < -1):
                        issues.append(f"Bad put delta {c['delta']}: ${c['strike']}")

            print(f"Pricing issues: {len(issues)}")
            for i in issues[:5]:
                print(f"  {i}")

        print(f"Buy result:     {'Success' if buy_result and buy_result.get('success') else 'Failed or N/A'}")
        print(f"Options news:   {len(news)}")
        print(f"Errors:         {len(errors)}")

        # Validation
        passed = True
        if not options_chain or options_chain.get("noChain"):
            print("WARN: No options chain received")
        if errors:
            print(f"FAIL: {len(errors)} errors")
            passed = False

        print(f"\n{'PASSED' if passed else 'FAILED'}")

asyncio.run(test())
