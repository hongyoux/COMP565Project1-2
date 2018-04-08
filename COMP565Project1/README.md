# README #

## Project 1 ##

### Team Members ###
Hongyou Xiong

### Program Requirements ###
1. Download the zipped up source code & content files from canvas and set up the project on your system
2. Pick a theme for your scene, find and create models that match your theme.
3. Create 4 or more modeled treasures and place them in various locations on the map. One treasure
must be placed within the “barrier” walls close to the inside corner (vertex (447, 453) position (67050,
67950)) but not within any wall “brick” bounding sphere.
4. Generate your terrain by modifying TerrainMap.cs and then use a paint programs to gaussian blur
effect to average or smooth the height and color textures.
5. Modify the player object and NPC to better move on the surface of the terrain by interpolation with
the Vector3.Lerp(. . . .) method. You must have an ‘L’ key defined to toggle Lerp or default terrain
following on/off. This way you can see the effect of Lerp.
6. The NPAgent currently follows a path following algorithm. The ‘N’ key should toggle the agent into
a treasure-goal seeking state.
7. You must write a detailed but concise description of EVERY feature implemented and exactly how
you implemented them.

### Theme ###

The theme I chose is a grass plains with small blue-ish huts. The huts will have a small decoration on top of it. The huts will be 4 x 4 x 5 inches (600x600x750). This seemed reasonable given the people in the simulation are 2 inches tall tall. I scattered 10 huts in an area that is similar to the clouds. In order to try and reduce the overlap, every time it spawns a hut it accumulates a random amount of x or z in vertex space that forces the huts to spawn further in that direction.

There will also be small shrubs. For project 1, I opted for slightly colored green spheres with minor extrusions and indents. My modeling technique isn't good enough for complex shapes but I wanted to add something more than just huts to the plains. I added 300 small bushes all over the map, with a caveat that if it was higher than 175 in height, it would reroll its x and z locations until it was not. This is to prevent green shrubs on top of the snowy mountains. 

### Treasures ###

The 4 types of treasures I went for were based on standard MMORPG treasures. The first one is a more traditional wooden treasure box. The second is a wooden crate. The third is a jar which can also serve as a barrel if I were to create a lid for it. And the fourth is a stone chest with some wooden planks on it. They are all sized appropriately within the bounds set. Each were sized between 150 to 300 pixels in all dimensions. I placed one in the inner corner of the "walls" as specified, one that was easily reachable nearby, one in the middle of mountainous range and one on the peak of a mountain. These were picked after walking my avatar to the location and recording the vertices found from the info panel. I figured with a variety of heights it would be more interesting than having all 4 in the testing quadrant.

### Terrain Algorithm ###

I applied the brownian motion algorithm provided on slides to produce a terrain. However, the results I encountered were the motion tended to fall flat in diagonals. I adjusted the algorithm for more variance and randomness by increasing number of passes from 6 to 9, and then changing the walk randomness. Instead of randomly choosing between -1 and 1, it chooses between -1 and 2. This gives the result more of a distributed randomness because it has a lower likelihood to wobble back and forth. I saved out the texture and applied a Gaussian blur of 7 much like the slides from lecture.

The height color map I did not make many changes to. I was quite happy with the original color scheme for a plains-esque theme. I adjusted it by having instead of dark green grass at height values < 5, I changed it to be brown at height 0.  The test quadrant area is now brown and it turns green quickly near the mountainous regions. Other than that, I changed the height bands from 25 to 50 for the middle values of height so that mountains felt more contiguously earthy colors as one color instead of multiple colors. I liked the snowy mountain tops so I did not make many changes there. I applied a Gaussian blur of 4 to the color image after creating the color texture. Both Gaussian blurs were applied through paint.net.

### Pathing Algorithm ###

I implemented a similar lerp pattern to the one mentioned in the slides of Lecture 5. I found A by dividing my current location by the stage's spacing and then multiplying again. With int clamping, this resulted in me rounding down the value to the nearest spacing (in this case, 150px). With A established, I was able to construct B and C, and run the algorithm of lerping AB and AC to get the difference in heights generated between the X and Z axis. Then added it to the existing height. This works pretty well in game. I've seen some discrepancies where sometimes the player will clip into the mountain, but my guess is that the height map doesn't align with the color map entirely due to the two Gaussian blurs being done separately.

### Treasure Seek Algorithm ###

I made some modifications to Stage.cs to store the treasures as a list and then added a function to find the closest treasure that's untagged relative to a location passed to Stage. I added a second NavNode value to NPAgent which did not belong on the path to store the value of the nearest treasure. Using an enum to toggle which search state it was in, I made two separate update functions to handle the original path follow algorithm and to handle the treasure seeking algorithm. The treasure seek would check each update to see if the distance between it and the nearest untagged treasure was below 200 + the bounding radius of the treasure. If so, it would mark the treasure and then switch states back to finding the next node in the path it was originally tracing.

### List of Models ###
All models were self-made in AC3D. All of them are stored in folder HongyouAdded in Content subdirectory.

- Bush.x
- Hut.x
- TreasureBox1.x
- TreasureBox2.x
- TreasureBox3.x
- TreasureBox4.x

### Modified AGMGSK classes ###

1. Agent.cs
  - Added int param "treasuresFound" to keep count of how many treasures an agent has found
  - Modified Update to check if a variable "isLerpActive" is true. If so, trigger the lerpHeight() function.
  - Created the lerpHeight function which updates height by lerping the height along the X and Z axis and averaging them relative to the current location of the player.

2. Inspector.cs
  - Modified InfoDisplayStrings to max of 25 strings
  - Added int param "ThirdBase" to set offset for a 3rd inspector panel.
  - Modified showInfo to mod 3 instead of mod 2 to account for the new inspector window I added.
  - Added another else if to Draw function to account for drawing the new Inspector panel.

3. MovableModel3D.cs
  - Added new bool param "isLerpActive" to keep track of if Lerp surface tracking is enabled.
  - Added function ToggleLerp() to toggle if lerp is active
  - Added function GetLerp() to check state of lerp.

4. NPAgent.cs
  - Added enum "NPAgentState" to track whether NPAgent was following the path or seeking treasure.
  - Added NPAgentState variable "state"
  - Added NavNode "treasureNav" to store treasure seeking location.
  - Added int "tagDistance" to track how close NPAgent has to be to "tag" the treasure.
  - Modified constructor to set state to path follow by default
  - Modified Update to check which state NPAgent was in and call respective update functions.
  - Refactored functional code from Update to PathSeekUpdate function
  - Created TreasureSeekUpdate to account for treasure hunting. Does this by getting closest untagged treasure from Stage and facing it.

5. Player.cs
  - Added const int "distanceToTagTreasure" and set to 200.
  - Modified update to check for nearest treasure. If said treasure was within 200 + bounding radius, tag treasure and increase treasuresFound.

6. Stage.cs
  - Modified InfoDisplayStrings to 25 from 20 to account for 3rd Inspector panel.
  - Added a list of Treasures "treasures" field;
  - Added internal function GetClosestTreasure which takes a translation and returns the closest untagged treasure relative to that position.
  - Modified Terrain construction to pull from HongyouAdded/heightTexture and colorTexture. Separated out added assets from base content assets with a folder.
  - Added information tracking on if lerp is active, if NPAgent is treasure or path following, which and how many treasures has each Agent found, and how many treasures were left untagged.
  - Added 4 treasure boxes to the map.
  - Added 300 bushes to the map.
  - Added 10 huts to the map.
  - Added keyboard inputs "L" and "N" to handle lerp and treasure seeking respectively.

7. Added Files
  - Huts.cs
    - File based off Cloud.cs to spawn many huts.
  - Bushes.cs
    - File based off Cloud.cs to spawn many bushes.
  - Treasure.cs
    - File to store treasure specific data such as whether it's been tagged.

8. Overall
  - Opened a bunch of files and ran Visual Studio 2017's "Format Document" so that auto-format would stop indenting randomly.

### Additional User Inputs ###

- "N"
  - Sets NPAgent to treasure seeking state. If there are no untagged treasures left, will not do anything and revert to path following algorithm.

- "L"
  - Sets NPAgent and Player's surface tracking from default "best approximation" to lerp method mentioned in lecture 5 slides.


## PROJECT 2 ADDENDUM ##

### Treasure Detection ###

In Project 1, I had already implemented the treasure search as a second update state for the NPAgent. As a result, all I needed to add was a check for distance to the closest treasure on every update, and if that number was below 4000, to switch into the TREASURE_SEEK state. Once all treasures were found, NPAgent's stepSize is set to 0 and state is set to DONE, which does nothing but return immediately on update. This effectively stops the bot from moving. In addition, I removed the components of the treasure from the list when I touch them now as a fix for Project 1. The treasure boxes will disappear on touch.

There is a pane in the inspector dedicated to the treasure seeking changes, and shows which treasures are taken and how many are taken by each, as well as remaining # of treasures.

In conjunction with the next big section for Object Avoidance, I also enabled collision detection for NPAgent by adding his object to the Collidables list and turning his IsCollidable to true.

I also made the starting nextNode and direction random, so that I could test obstacle avoidance from different points in the path.

### Obstacle Avoidance ###

Obstacle avoidance is a function that is run on every update event and starts by checking if stateNSteps is a value higher than 0. If so, then just keep going in the same direction. This is used by various states to force NPAgent around corners, or to back up N steps before attempting a different set of actions. In addition, every update we check a bunch of different variables to see determine our current state. The "goalVisible" state is set to true if there is at least 2 steps in the direction of the goal without running into a collision. The "wallAhead" state is true if there is a collision within the next 2 steps in the current direction I am moving. The reason I buffer these with a bit of space is because a sphere is round, its possible that we can enter a location where the spheres interlock in a way that makes it hard for the NPAgent to fix itself.

LeftSensorHit and RightSensorHit are states set to true if any collidable objects on the map are touching them. And then finally we have InWall, which is set to true if either sensor is outside the bounding box of the movable area. This is because the walls are not collidables, and therefore require a different rule.

With all these sensors and states set, there ended up being a total 10 states: ["GO_FORWARD", "BACK_UP", "TURN_RIGHT", "FOLLOW_WALL_LEFT", "LEFT_CORNER", "CORNER_PASS_LEFT", "TURN_LEFT", "FOLLOW_WALL_RIGHT", "RIGHT_CORNER", "CORNER_PASS_RIGHT"]. Forward would check if the goal was moveable; if so, then it would attempt to resume going straight toward the goal. Otherwise if it detected a wall ahead, whichever sensor found the wall it would enter their respective turn states. Turn states would turn until the sensor was no longer touching the wall, and then would turn back the other way just one step. This was so that the sensor would be in contact with the wall and it would help to follow the wall. Follow_wall would do one of three things: if the agent was angled into the wall still, it would turn back out and readjust its alignment with the wall. If the agent was not and the left sensor was still touching the wall, it would simply move forward. If it lost touch of its left_sensor, it would check to see if it was at a corner. THe corner states would move forward for another second before attempting to turn left until it hit the wall (went around the corner). Otherwise it would spin for a second, and then try to move forward again. This would reset the state to goal tracking if it had made it past the wall it was attempting to follow. The backup state was set when it entered into a loop in states. This would turn the robot 180 degrees and have it move for 60 frames back. Then turn 90 degrees and move another 60 frames, before turning another 90 degrees and reseting the state to FORWARD. this would essentially move it away from any corners that could've trapped it before and gave it another attempt at traversing the obstacles.

I've included the flowchart in the parent folder and hopefully that is sufficient for explaining the state machine I ended up building. I opted to not flesh out the TURN_LEFT states because they are the same as the TURN_RIGHT set of states, but with the opposite turns and signals. It was simply unwieldy to fit into the chart without making a mess. It is not the most elegant solution but it seemed to traverse the map to the different treasures relatively easily. I was unsure what you meant by a complex path to the treasure, but I set up multiple different shapes of obstacles around the map like a concave wall and a diagonal. One issue I did run into was with heights, so I ended up flattening all the spheres into circles and checking as if Y was not considered. This helped in the case of the diagonal obstacles along the mountainous areas.

### Flocking ###

For Flocking, I chose to use a leader-based flocking system. The leader is the player character. I tried to do the flocking with no unit of visibility, and that all members of the flock influenced each other. My assumption was that this would create a single flock and that no subflocks would spawn. The cohesion direction is the normalized direction from the dog to the player. The separation direction was determined by adding together the direction between any two boids * a separation force of 1500 units and then dividing by the distance squared. This meant that boids that were closer felt much more separation force. For the alignment force, I took the normalized vector of the leader's alignment.

After generating these forces, I used the weights mentioned in the slides to generate a weight for each cohesion separation and alignment factors. These were then multiplied against the forces to get a direction of movement. Taking the dot product between this and the dog's current facing, that would generate an angle by which to rotate the yaw of each dog. This leads to some weird spinning dogs but for the most part they seem to be flocking around a point that tends to be in front of the player character. The general vicinity is there. Unfortunately I ran low on time to polish this portion of the project so I will be looking to incorporate some amount of flocking in Project 3 if possible.

I ended up choosing to roll a die at the start of every update, which determined if the dog was going to perform its old behavior of spinning around a bunch, or if it would use the packing movement. I added a pane in the inspector for flocking after the obstacle avoidance pane.

### Classes I changed ###



### Additional User Input Commands ###

