# Lanboost's NavMesh

A personal simplified version of NavMesh and NavMesh creation in 2D. Think "simple" "recast and detour".

Both generation and usage of the NavMesh is chunk-based. Load what you need, and nothing else.

## Algorithms
### Generation
- Generate NavMesh with basic tile based "recast" in a 2D world. (Could be extended to 3D)
### PathFinding
- Starts with A* to find touched NavMeshes
- String pulling on the NavMesh path with Simple Stupid Funnel (SSF)
- (No path smoothing)
 
## Example

Godot example in the example folder, not user friendly and is mostly used for debugging purposes.

## Dependencies
- [LanboostPathfindingSharp](https://github.com/Lanboost/LanboostPathfindingSharp)
### Sub-Dependencies
- OptimizedPriorityQueue