# AStar-Unity-Mashup

This project does not use Unity's built-in `NavMesh`, instead using a home-grown A* algo.

This project is currently not "playable" in any sense of the word. It is a technical demo, until further notice (probably forever).

## Latest awesome gif-animated visuals

![Rotation in Isometric View](https://raw.githubusercontent.com/wiki/jstine35/AStar-Unity-Mashup/images/astar-rotation-overlay.gif)

I threw a fairly pointless user control hint up, mostly to familiarizew with Unity UI Panels and TextMeshPro.

## Why... ?

This started as a by-request interview test. Afterward I decided to make a best effort to turn it into something more portfolio-suitable. It now serves as an
opportunity for me to improve my first-hand experience with Unity.

_(you are welcome to infer the outcome of the interview)_

The original defined criteria was to play with the basic concepts of path finding through a simple 2D landscape, without using Unity's built in `NavMesh`.
The rationale for why we aren't using `NavMesh` is for the sake of "academia." Also, see long term goals for potentially other reasons.

To accomplish this within a budgeted timeframe, I decided to implement a rather un-Unity-like approach, which plotted two GameObjects and pushed one toward
the other along a list of waypoints obtained from the A* algo. Since then, I have been applying more appropriate mouse and keyboard-driven controls to
improve the UX factor of this tech demo.

## Long term goals

### Pathfinding

 * Improve tooling and visualization, so that broken paths can be understood and remedied
 * Implement pathfinding at scales formerly unknown to Unity (tens of thousands of tiles)
 * multi-level pathfinding along isometric slopes

### Gameplay

 * Figure out some kind of goal / purpose
 * Add some achievements feedback, let the player feel awesome for a moment
 * try to avoid "scores" (boring!)
 * Real stickman models.
 * amazing advanced animations, sound fx, and a live orchestra soundtrack composed by Skaven (Peter Hajba)
 
 _(last one might be a bit of a stretch goal)_
