# GEAYI Nativo — Plan del proyecto

Reescritura nativa del juego "GEAYI: Obby Xtreme 3D" en Unity (C#) para Android.
El juego web (Three.js) sigue en desarrollo y pruebas; este proyecto corre en paralelo.

## Estado
- [x] Estructura del proyecto creada
- [ ] Scripts base (en progreso — agente trabajando)
- [ ] Escena principal de la ciudad
- [ ] Jugador + cámara + controles táctiles
- [ ] Streaming (solo mostrar lo cercano)
- [ ] Vehículos
- [ ] Modo construir sin límites
- [ ] Multijugador base
- [ ] Publicar en Play Store

## Fases
1. **Base** — Proyecto Unity, scripts core, escena vacía con jugador que camina
2. **Ciudad** — Calles, árboles, farolas con streaming
3. **Sistemas** — Tienda, poderes, mascotas, vehículos
4. **Construir** — Modo construir + guardar
5. **Online** — Multijugador
6. **Tienda** — Compilar APK y publicar en Play Store

## Reglas (del juego original)
- Sin límites de construcción
- Mundos bajos, al nivel del suelo
- Todo original, nada copiado
- Optimizado para teléfonos baratos (menos de 150 draw calls, streaming a 90m)
- Fácil de jugar, que no se estresen

## Notas técnicas
- Motor: Unity 2022+ (LTS)
- Lenguaje: C# (los juegos usan Luau/Lua en Roblox; aquí C#)
- Render: URP (Universal Render Pipeline) para móviles
- Guardado: PlayerPrefs + JSON
- Red: Netcode for GameObjects (cuando toque)
