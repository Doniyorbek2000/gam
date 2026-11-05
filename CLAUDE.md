# BilliardGame
Mobile 3D billiards (8-ball first, later 9-ball/snooker) with PUBG Mobile–style meta: collectible cues/tables/effects, crate rolls (gacha), online 1v1 with friends, in-match voice chat.

## Stack
- Client: Unity 6 (6000.5.x), URP, C#, new Input System, TextMeshPro. Android first (API 26+, IL2CPP, ARM64), iOS later.
- Backend: Nakama (self-hosted, Docker), server modules in TypeScript.
- Voice: Unity Vivox (fallback LiveKit). Admin: NestJS + Next.js (separate repo).

## Architecture rules
1. Physics = own 2D sim in BilliardGame.Physics, no UnityEngine refs, fixed step 1/240 s. Input ShotParams, output frames + events.
2. Online = server-authoritative: client sends only ShotParams, server simulates and broadcasts frames/events.
3. Economy (coins, gems, inventory, crate rolls, IAP validation) only on server. Crate odds shown in UI.
4. Presentation code holds no game rules; rules live in BilliardGame.Gameplay.

## Conventions
Namespaces BilliardGame.<Area>; [SerializeField] private fields; no per-frame allocations in sim; no LINQ in Update; one .asmdef per Scripts subfolder; Physics and Gameplay are editor-testable (NUnit).

## Roadmap
1. Offline MVP (table, sim, cue aim/power/spin, 8-ball rules, hot-seat)
2. Online 1v1 via Nakama  3. Economy & skins & crates  4. Voice chat  5. Polish, ranked, IAP
