# Solar System & Planetary Flow Motion Simulation

A 3D planetary and celestial motion simulation built with Unity, exploring the physics and visuals of **Planetary Flow Motion**.

---

## 🌌 Core Concept: Planetary Flow Motion

> *Planetary motion describes how planets move through space, driven by two primary actions: **orbiting around the Sun** and **spinning on their internal axes**. These movements follow precise physical laws and create recognizable flow patterns in the night sky.*

---

## 🚀 Key Features & Architectural Pillars

### 1. Celestial & Keplerian Orbital Dynamics
* **Elliptical Trajectories**: Accurate elliptical orbital mechanics parameterized by eccentricity ($e$) and inclination ($\theta$).
* **Keplerian Speed Scaling**: Dynamic velocity variations governed by Kepler's 2nd Law ($v \propto \frac{1}{r^2}$), accelerating near perihelion and decelerating near aphelion.
* **Color-Coded Orbit Trails**: Real-time 3D rendered orbital trajectories around the Sun.

### 2. Rotational Mechanics & Atmospheric Flow
* **Axial Tilt Precision**: Internal spin axes tilted according to planetary measurements (Earth $23.4^\circ$, Mars $25.2^\circ$, Uranus $97.8^\circ$).
* **Procedural Atmospheric Flow Shaders**: Dynamic UV vector flow motion simulating swirling cloud layers, ocean currents, and gas giant atmospheric storm bands.

### 3. Smooth Camera & Interactive Flow Controls
* **Momentum Damping**: Smooth exponential damping on camera panning, orbital rotation, and scroll zooming.
* **Seamless Target Focusing**: Ease-in-out target transitions for tracking individual planets and moons.

---

## 📜 System Scripts Overview

* [`CelestialBody.cs`](Assets/Scripts/CelestialBody.cs): Handles orbital motion, Keplerian speed calculations, axial rotation, and 3D orbit line rendering.
* [`PlanetaryFlowShader.cs`](Assets/Scripts/PlanetaryFlowShader.cs): Manages dynamic UV flow animation for planetary atmosphere and cloud textures.
* [`PlanetCameraController.cs`](Assets/Scripts/PlanetCameraController.cs): Ultra-smooth camera tracking and orbit interaction controller.
* [`SolarSystemBootstrapper.cs`](Assets/Scripts/SolarSystemBootstrapper.cs): Automatically sets up solar system bodies, starfields, asteroid belts, and lighting on scene load.
* [`SolarSystemUIManager.cs`](Assets/Scripts/SolarSystemUIManager.cs): Renders educational UI overlays and info cards.
"# solarSystem1" 
"# solarSystem1" 
