# BilliardGame
Professional, realistic mobile 3D billiards (8-ball, then 9-ball, snooker) at PUBG Mobile / eFootball quality, with a full meta and monetization ecosystem: premium venues, 3D avatars, cosmetics (Common → Mythic, upgradable Mythics), crates with public odds and pity, Season Pass, ranked, tournaments, clubs, in-match voice chat.

## Docs (source of truth — read the relevant one before working)
- [docs/GDD.md](docs/GDD.md) — modes, venues, core/meta loop, progression, social
- [docs/ECONOMY.md](docs/ECONOMY.md) — currencies, prices (UZS/USD), crate odds, pity, Season Pass, revenue model
- [docs/UI_UX.md](docs/UI_UX.md) — screen layouts (lobby, shop, crate opening, inventory, pass, HUD, results)
- [docs/ART_PIPELINE.md](docs/ART_PIPELINE.md) — import flow, poly/texture budgets, license policy, asset requests
- [docs/TECH_ARCHITECTURE.md](docs/TECH_ARCHITECTURE.md) — client, Nakama server, NestJS/admin/web store, data model, RPCs, payments
- [docs/COMPLIANCE.md](docs/COMPLIANCE.md) — Play Payments, loot box laws, ratings, privacy, licenses
- [docs/ROADMAP.md](docs/ROADMAP.md) — milestones M1… with acceptance criteria and status

## Stack
- Client: Unity 6 (6000.5.x), URP, C#, new Input System, TextMeshPro, Addressables (remote skins/venues via CDN). Android first (API 26+, targetSdk 36, IL2CPP, ARM64), iOS later.
- Backend: Nakama 3.41 (self-hosted, Docker), server modules in TypeScript (`Server/nakama/modules`). Remote Config = Nakama `config` storage.
- Voice: Unity Vivox (fallback LiveKit). Admin: `admin/api` (NestJS) + `admin/web` (Next.js); web store: `web/` (Next.js); infra: root `docker-compose.yml` + `deploy/` (Contabo, Caddy) — all in this repo.
- Characters (M1.6): Humanoid rig, one Animator controller, Animation Rigging IK, Cinemachine 3, NavMesh; purely visual — never feed back into physics, rules or timers.

## Architecture rules
1. Physics = own 2D sim in BilliardGame.Physics, no UnityEngine refs, fixed step 1/240 s. Input ShotParams, output frames + events.
2. Online = server-authoritative: client sends only ShotParams, server simulates and broadcasts frames/events. C# and TS physics/rules stay bit-exact (`npm test` cross-check).
3. Economy (coins, gems, inventory, crate RNG, pity, IAP/web payment validation) only on server, every currency change through the wallet ledger with reason + idempotent request_id. Client economy code only displays (mock allowed in editor/M1).
4. Presentation code holds no game rules; rules live in BilliardGame.Gameplay.
5. No pay-to-win: cosmetics never reach ShotParams, PhysicsConfig, aim-line length, shot timer or matchmaking.
6. Crate odds shown before purchase are generated from the server table, never hand-written.
7. Build flavors: `STORE_PLAY` (Google Play Billing only — no web store/Payme/Click code or text), `STORE_DIRECT` (site APK, local payments), `STORE_MOCK` (dev).
8. Assets: only CC0, CC-BY, Sketchfab Standard, CGTrader Royalty Free, OFL (UI icons also Apache-2.0; animations/characters also Mixamo downloaded directly from mixamo.com — never third-party re-uploads, never game rips); every asset logged in `ArtSource/LICENSES.md` (Credits are generated from it). No NC/Editorial/ND/SA, no real people, no real brand logos.

## Conventions
Namespaces BilliardGame.<Area>; [SerializeField] private fields; no per-frame allocations in sim; no LINQ in Update; one .asmdef per Scripts subfolder; Physics, Gameplay and Meta.Rules are editor-testable (NUnit, no UnityEngine). Mobile budgets: table ≤ 40k tri, cue ≤ 5k, venue ≤ 150k, LOD0–2, ASTC textures 1K/2K.

## Workflow
When asked "ROADMAP.md dagi keyingi milestone'ni bajar": take the first 🟨 (started) or ⬜ milestone in docs/ROADMAP.md, implement it, verify in Unity via MCP (play mode, screenshots, Console clean) plus EditMode tests and `npm test`, mark it ✅ in ROADMAP.md, commit, push to `origin main` (GitHub, art binaries via Git LFS), and give a short report (in Uzbek). Split into Mx.1/Mx.2 in ROADMAP.md if it does not fit one session.
