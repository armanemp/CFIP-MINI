# CFIP-MINI Master Roadmap

## Phase 0 — Foundation
- [x] Standalone repository
- [x] No execution/order management
- [x] Performance-first architecture
- [x] Single-source-of-truth level model

## Phase 1 — Market Level Engine
- [x] Previous day OHLC
- [x] Previous day midpoint / quartiles
- [x] Current day open/high/low
- [x] Previous week OHLC
- [x] Weekly midpoint / quartiles
- [x] Session high/low/open/mid
- [x] Initial Balance
- [x] ATR range extensions
- [x] Premium / discount

## Phase 2 — Profile Engine
- [x] Estimated tick-volume profile
- [x] POC
- [x] VAH / VAL
- [x] HVN / LVN
- [x] Previous-day profile levels

## Phase 3 — Structure & Liquidity
- [x] Confirmed swing highs/lows
- [x] HH / HL / LH / LL classification
- [x] Equal highs/lows
- [x] Liquidity clusters
- [x] Breakout / sweep context

## Phase 4 — Reaction & Confluence
- [x] Level proximity
- [x] Touch / penetration / close state
- [x] Rejection / acceptance
- [x] Breakout / retest context
- [x] Reversal vs continuation score
- [x] Confluence zones
- [x] Nearest target map

## Phase 5 — Hardening
- [ ] Validate against multiple symbols
- [ ] Validate on M1/M5/M15/H1
- [ ] Profile performance with deep history
- [ ] Visual clutter audit
- [ ] API compatibility audit against installed cTrader build
- [ ] Add regression fixtures after live-chart validation

## Non-goals
- No auto trading
- No order placement
- No position management
- No internet/news dependency
- No duplicated signal engine
