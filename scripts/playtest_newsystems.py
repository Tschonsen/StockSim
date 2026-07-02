"""Playtest focused on the news<->company depth systems (Points 1-7).
Connects to the running backend, drives the sim at max speed, and scans the news
stream for the new behaviours + any instability (errors / NaN / dead prices)."""
import asyncio, json, sys, time, math

PORT = int(sys.argv[1])
TARGET_DAYS = 15

SECTOR_METRIC_SIGNS = [
    "net interest margin", "loan growth", "trading revenue", "pipeline progress",
    "drug sales", "trial readouts", "comparable sales", "volume growth", "pricing gains",
    "subscriber", "ARPU", "broadband", "freight", "load factor", "yield growth",
    "occupancy", "rental income", "leasing", "production volumes", "refining margins",
    "upstream output", "order intake", "backlog growth", "equipment demand",
    "shipment volumes", "realized prices", "rate-base", "regulated earnings",
    "load growth", "brand momentum", "flagship demand", "cloud revenue", "enterprise bookings",
]

async def main():
    uri = f"ws://127.0.0.1:{PORT}"
    import websockets
    async with websockets.connect(uri, open_timeout=15, max_size=None) as ws:
        await ws.send(json.dumps({"type": "NewGame", "payload": {
            "playerName": "DepthBot", "startingCash": 100000, "stockCount": 30, "seed": 42}}))
        await ws.send(json.dumps({"type": "SetSpeed", "payload": {"speed": 5}}))
        await ws.send(json.dumps({"type": "UpdateSettings", "payload": {"SkipWeekends": True}}))

        days, errors, news = set(), [], []
        types_seen, current_day = {}, None
        bad_prices = []  # (sym, price) NaN/neg
        first_prices, last_prices = {}, {}
        sectors = {}

        for _ in range(200000):
            try:
                data = json.loads(await asyncio.wait_for(ws.recv(), timeout=20))
            except asyncio.TimeoutError:
                print("TIMEOUT — stopping"); break
            t = data.get("type", "?"); p = data.get("payload", {})
            types_seen[t] = types_seen.get(t, 0) + 1

            if t == "MarketSnapshot":
                for s in p.get("stocks", []):
                    sym = s.get("symbol", "?")
                    sectors[sym] = s.get("sector", "?")
                    first_prices.setdefault(sym, s.get("price", 0))
                # initial news in snapshot
                for e in p.get("initialNews", []) or []:
                    news.append(e)
            elif t == "MarketUpdate":
                gt = p.get("gameTime", "")
                day = gt[:10]
                if day and day != current_day:
                    current_day = day; days.add(day)
                for u in p.get("prices", []):
                    sym = u.get("Symbol", u.get("symbol", "?")); pr = u.get("price", 0)
                    last_prices[sym] = pr
                    if pr is None or (isinstance(pr, float) and (math.isnan(pr) or math.isinf(pr))) or pr < 0:
                        bad_prices.append((sym, pr))
                if len(days) >= TARGET_DAYS:
                    break
            elif t == "NewsEvents":
                for e in p.get("events", []):
                    news.append(e)
            elif t == "error":
                errors.append(p); print("ERROR:", str(p)[:160])

        # ---- analysis ----
        def text(e): return " ".join(str(e.get(k, "")) for k in ("headline", "summary", "analystQuote"))
        traj = [e for e in news if "The move comes as" in text(e)]
        persona = [e for e in news if "leans into growth" in text(e) or "turns defensive" in text(e)
                   or "strategy_shift" in str(e.get("tags", ""))]
        sect = [e for e in news if any(sig in text(e) for sig in SECTOR_METRIC_SIGNS)]
        grounded_q = [e for e in news if e.get("analystQuote") and
                      ("x earnings" in e["analystQuote"] or "net margin" in e["analystQuote"]
                       or "revenue up" in e["analystQuote"] or "revenue growth at" in e["analystQuote"])]
        blockchain_health = [e for e in news if "blockchain" in text(e).lower()
                             and sectors.get((e.get("affectedSymbols") or ["?"])[0]) == "Healthcare"]

        print("\n" + "="*72)
        print("NEW-SYSTEMS PLAYTEST")
        print("="*72)
        print(f"Days simulated:        {len(days)}")
        print(f"Message types:         {types_seen}")
        print(f"Total news captured:   {len(news)}")
        print(f"Errors:                {len(errors)}")
        print(f"Bad prices (NaN/neg):  {len(bad_prices)}  {bad_prices[:5]}")
        print(f"\n-- new text systems --")
        print(f"Trajectory clauses (P4):     {len(traj)}")
        print(f"Persona-shift news (P3b):    {len(persona)}")
        print(f"Sector-metric news (P6):     {len(sect)}")
        print(f"Grounded analyst quotes (P5):{len(grounded_q)}")
        print(f"Healthcare 'blockchain' bug: {len(blockchain_health)} (want 0)")

        def show(label, items, n=3):
            print(f"\n### {label} (showing {min(n,len(items))} of {len(items)})")
            for e in items[:n]:
                print(" •", (e.get("headline","") or "")[:140])
                if e.get("summary"): print("   ", e["summary"][:220])
                if e.get("analystQuote"): print("    Q:", e["analystQuote"][:200])
        show("Trajectory (P4)", traj)
        show("Sector-metric headlines (P6)", sect)
        show("Grounded analyst quotes (P5)", grounded_q)
        if persona: show("Persona evolution (P3b)", persona)

asyncio.run(main())
