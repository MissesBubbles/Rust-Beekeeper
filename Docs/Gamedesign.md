# Beekeeper Game Design

## Plugin Identity

Beekeeper is a Rust server plugin that adds a configurable beekeeper NPC, bee-themed survival items, honey selling, and a beehive care/progression system.

The goal is to support hardcore and slow-progression servers without giving players overpowered resources.

---

## Core Features

### Beekeeper NPC
- Admin-placeable NPC
- Configurable name
- Configurable location
- Configurable shop
- Configurable honey buyback
- Optional multiple beekeeper support later

### Honey Selling
- Players can sell jars of honey to the beekeeper
- Admins can set required honey amount
- Admins can set reward item and reward amount
- Admins can enable or disable selling
- Admins can set daily sell limits

### Custom Items
- Honey Ration
- Bee Jelly
- Pollen Pouch
- Wildflower Seeds
- Bee Nucleus
- Beekeeper Gloves
- Bee Box
- Apiary Starter Kit

### Beehive System
- Hives have health
- Hives can be maintained
- Hives can become sick
- Hives can die
- Hive quality affects production
- Bee Nucleus quality affects hive strength

---

## Design Rule

The plugin should feel useful, slow, and rewarding, but never overpowered.

# Core Gameplay Loop

1. Find the Beekeeper
2. Purchase your first Apiary Starter Kit
3. Place your first hive
4. Collect pollen
5. Plant wildflowers
6. Harvest honeycomb
7. Produce honey
8. Sell honey to the Beekeeper
9. Earn scrap
10. Upgrade your apiary
11. Find better Bee Nuclei
12. Build the best apiary on the server


## Loot Spawning

Admins can optionally allow bee items to appear in loot containers.

Supported loot options:
- Locked Crate
- Elite Crate
- Military Crate
- Custom container shortnames later

Each item can have:
- Enabled/disabled loot spawning
- Drop chance
- Minimum amount
- Maximum amount
- Allowed containers