# CFIP-MINI — Professional Market Level Map

CFIP-MINI is a standalone, lightweight cTrader market-structure and level-mapping indicator.

It is intentionally independent from CFIP. It does **not** place orders, execute trades, manage positions, or act as a trading robot.

## Goal

Turn a raw chart into a compact decision map:

- Previous/current day and week reference levels
- Session ranges and Initial Balance
- Estimated volume-profile POC / VAH / VAL
- HVN / LVN acceptance and rejection areas
- Swing structure and meaningful liquidity
- Equal highs/lows and liquidity clusters
- Premium / discount
- ATR range extensions
- Level confluence zones
- Price reaction state: rejection / acceptance / breakout / retest / continuation / reversal risk
- Nearest target map
- Compact status panel

## Design rules

1. One calculation owner per behavior.
2. Calculate only when necessary; do not rebuild the whole history on every tick.
3. Stable chart-object names; update/reuse instead of creating unlimited objects.
4. Historical depth is bounded.
5. No network access.
6. No execution logic.
7. No forced BUY/SELL signals. The tool describes market context and reaction.
8. Forex/CFD volume-profile values are based on broker tick volume and are labelled as estimates.
9. Confluence is preferred over a forest of overlapping lines.
10. All visible conclusions come from the same calculated level/reaction model.

## Current engine

- Time levels
- Session engine
- Profile engine
- Structure engine
- Liquidity engine
- Confluence engine
- Reaction engine
- Target map
- Compact panel

## Display modes

- Clean: strongest levels/zones only
- Balanced: normal professional view
- Full: expanded analytical map

## Important interpretation

A level is not automatically support/resistance. The indicator evaluates what price does around it:

- Rejection -> reversal pressure increases
- Acceptance -> continuation pressure increases
- Sweep -> liquidity event; wait for confirmation
- Breakout + retest -> continuation context
- Repeated failure -> level weakens
- Confluence -> importance increases

Profile levels use tick volume as a broker-data proxy; they are not exchange volume.

## Source

The implementation targets cTrader Algo / .NET 6 and uses the documented MarketData.GetBars and chart drawing APIs.
