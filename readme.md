# WORK IN PROGERS

<p align="center">
  <img src="logo.png" width="400" alt="OXL">
</p>
# OXL — Online eX-Owner Lies 🚗💸

> *"One owner. Runs great. Selling only because I bought a new one."*

**OXL** is an in-game car auction website mod for **Car Mechanic Simulator 2026**.
Browse listings, spot a deal, buy a wreck — then fix it up and sell it for profit.


## ✨ Features

- **Live Listings** — 4 to 10 active auctions at any time, refreshed dynamically as
  in-game time passes. Miss the window and the deal is gone.
- **Filters** — narrow down by make and year
- **Seller Descriptions** — every listing comes with a suspiciously optimistic write-up
  from the previous owner
- **One-click Purchase** — car spawns in your parking lot, cash is deducted instantly

---

## 📥 Installation

**Required:**
- [MelonLoader v0.7.2+](https://github.com/LavaGang/MelonLoader)
- [_CMS2026_UITK_Framework](link)

**Optional:**
- [CMS 2026 Simple Console](link) — enables the `oxl_open` command

**Steps:**
1. Drop `_CMS2026_UITK_Framework.dll` into your `Mods/` folder
2. Drop `OXL.dll` into your `Mods/` folder
3. Launch the game

---

## 🚀 Usage

| Action | How |
|---|---|
| Open / close panel | **F9** |
| Filter listings | Dropdowns at the top of the panel |
| Buy a car | *Buy* button on any listing |
| Console command | `oxl_open` *(requires Simple Console)* |

---

## 🗺️ Roadmap

- [x] Browse listings with filters
- [x] Purchase a car — spawns on parking lot, deducts cash
- [x] Dynamic auctions with time limits
- [ ] Seller personalities and backstories
- [ ] Chat with seller and price negotiation
- [ ] List your own cars for sale through OXL

---

## ⚠️ Known Limitations

- Panel renders below the native Canvas during scene transitions (Unity architecture)
- Demo car limit is capped at 10 — purchase is blocked when the lot is full

---

## 📄 License

MIT — part of the CMS 2026 modding ecosystem.

---

*Built on [CMS2026 UITK Framework](link) — the community UI library for CMS 2026 mods.*