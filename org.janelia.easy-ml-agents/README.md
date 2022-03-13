# Janelia Easy ML-Agents

## Summary

This package (`org.janelia.easy-ml-agents`) simplifies the setup of reinforcement learning using the [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents) package (`com.unity.ml-agents`) version 2.  ML-Agents is well documented, but it is easy to forget some of the steps necessary to get it working, and omitting these steps will not necessarily produce errors or warnings.  With `org.janelia.easy-ml-agents`, the "agent" being trained and the "arena" in which training occurs can be derived from base classes, which either handle the setup steps themselves or produce error messages if the derived classes omit necesssary steps.

The setup of ML-agents that is supported by this package is based on the ["ML-Agents: Hummingbirds" course](https://learn.unity.com/course/ml-agents-hummingbirds).

## Installation

To install this package for use by Unity C# scripts, follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation) for the case "Without Dependencies".  This package depends on the [`com.unity.ml-agents` package](https://docs.unity3d.com/Packages/com.unity.ml-agents@2.0/manual/index.html), but the standard Unity Package Manager will install that dependency automatically.

To actually perform training, additional Python code must be installed outside of Unity.  The process is straightforward and well documented in the [ML-Agents installation instructions](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Installation.md).  These instructions involve creating a
[Python Virtual Environment](https://github.com/Unity-Technologies/ml-agents/blob/release_19_docs/docs/Using-Virtual-Environment.md); at least on Windows, the alternative of using [Conda](https://docs.conda.io/en/latest/) for package management [seems to be deprecated](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Installation-Anaconda-Windows.md).  Note that on Windows, [PyTorch must be installed first in a separate step](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Installation.md#windows-installing-pytorch).

The current version of `org.janelia.easy-ml-agents` has been tested with `com.unity.ml-agents` version 2.0.1 (the version avaliable through the Package Manager as of February, 2022), PyTorch version 1.7.1 and the `mlagents` Python package version 0.28.0.

Note that installing `org.janelia.easy-ml-agents` does trigger the installation of `com.unity.ml-agents` due to dependencies.  But for some reason, the Package Manager window does not display com.unity.ml-agents in the "Packages: In Project" tab the same way it would if `com.unity.ml-agents` were installed manually. 

## Usage

To set up a trainable arena and agent, use the following steps.  Missing a required step will produce a compilation or runtime error, except as noted.

### 1: Subclass from `Janelia.EasyMLArena`

The subclass of `Janelia.EasyMLArena` should override two methods of the base class.   

#### 1A: Override `Setup(IEasyMLSetupHelper helper)`

This method performs the initial setup of the arena triggered by the "Create Easy ML Arena and Agent" menu item from the Unity editor's "GameObject" menu.  

All the objects involved in training should be made children of the `EasyMLArena`'s `transform`.  Using this hierarchy supports training with [multiple areas](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Learning-Environment-Design.md#multiple-areas), where each area is an instance of the arena with its own agent and all agents train concurrently.  An agent that wants to observer its distance to a target object, for example, chooses the correct target by looking in its arena's hierarch.  It is convenient to find objects by their [tags](https://docs.unity3d.com/Manual/Tags.html), but the [standard Unity API for tags](https://docs.unity3d.com/ScriptReference/GameObject.FindGameObjectsWithTag.html) does not work relative to a top-level object.  Instead, use the helper functions `Janelia.EasyMLRuntimeUtils.FindChildrenWithTag` or `Janelia.EasyMLRuntimeUtils.FindChildWithTag` (from `EasyMLRuntimeUtils.cs`).

The `Setup` method could process (e.g., reparent) objects that were created separately with the Unity editor, or it could create all the objects in the environment itself.  For the latter approach, `org.janelia.easy-ml-agents` provides helper functions for activities often perfomed with the Unity user interface, like assigning a custom mesh to an object, generating a material that gives a mesh a color, creating a [tag](https://docs.unity3d.com/Manual/Tags.html) to identify an object, etc.  These functions are available to `Setup` through its `helper` argument; see `EasyMLSetupHelper.cs` for more details.

#### 1B: Override `PlaceRandomly()`

This method is executed at the start of each training episode, and it should introduce scene variability to ensure that training is generalizable to different conditions.  It might incorporate [curriculum learning](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Training-ML-Agents.md#curriculum), where the difficulty of training increases gradually so it is neither too difficult at the start nor too easy at the end.  For example, a ball to be chased by the agent might have its angular drag decreased gradually so it moves less at the start of training.  ML-Agents supports curriculum learning through [environment parameters](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Learning-Environment-Design.md#environment-parameters) that are specified in the `trainer_config.yaml` file and accessed in the arena and agent code.  The `org.janelia.easy-ml-agents` package does not add any additional capabilities beyond the standard approach.

### 2: Subclass from `Janelia.EasyMLAgent`

The subclass of `Janelia.EasyMLAgent` should override several properties and methods of the base class.  

#### 2A: Consider Using `Janelia.EasyMLAgentGrounded`

If the agent is meant to move along a flat ground plane, then the agent could be a subclass of `Janelia.EasyMLAgentGrounded`.  That class takes care of some details needed by a subclass of `Janelia.EasyMLAgent`.  It gives the agent a wedge-shaped body mesh (pointed at the front), with a simple box collider.  It overrides the `Heuristic` method to support control of the agent's motion via the keyboard arrow keys, and overrides the `OnActionReceived` method to move the agent with simple forces.  An additional subclass of `Janelia.EasyMLAgentGrounded` then can add the additional overrides, like an `OnActionReceived` that adds rewards (after calling `base.OnActionReceived`).

#### 2B: Override `string BehaviorName`

This name should match the name in [the `behaviors` subsection](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Training-ML-Agents.md#behavior-configurations) of the [training configuration YAML file](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Training-Configuration-File.md), like `FetchVersion2` in the following example:
```
behaviors:
  FetchVersion2:
    trainer_type: ppo
    hyperparameters:
      batch_size: 2048
...
```
Unfortunately, `org.janelia.easy-ml-agents` currently has no way to report an error if these names do not match.

#### 2C: Override `int VectorObservationSize`

This value should match the number of observations passed with `AddObservation` in the overridden `CollectObservations` method (see below).  Note that a `Vector3` passed to `AddObservation` counts as three observations.

#### 2D: Override `int VectorActionSize`

This value should match the number of continuous actions added in the overridden `Heuristic` method and processed in the overridden `OnActionReceived` method (see below).  Note that a `Vector3` counts as three actions because each component is added separately.

#### 2E: Override `Vector3 ColliderSize`

This value is the size of the `BoxCollider` given to the agent.  Note that a value _S_ given in any of the dimensions means that the box covers the range [-_S_/2, _S_/2] in that dimension.  A value of `Vector3.zero` signals that the `BoxCollider` should not be added.

#### 2F: Override `void Setup(IEasyMLSetupHelper helper)`

This method performs initial setup of the agent (and is called after the `Setup` method for the parent arena).  To see an example of what this method might do, look at `Janelia.EasyMLAgentGrounded.Setup`, which performs a few operations like adding the agent's body mesh.

#### 2G: Override `void OnActionReceived(ActionBuffers actions)`

This method takes each action value `actions.ContinuousActions[i]` and uses it to update the activity of the agent.  This method is also an appropriate place to call `AddReward` since this method is called at each step of the training.  In `Janelia.EasyMLAgentGrounded.OnActionReceived`, for example, the `actions.ContinuousActions[0]` value is forward speed which is converted to a force on the agent's `Rigidbody`, and 
and `actions.ContinuousActions[1]` is yaw rotation which is applied to the agent's `Transform`.  The `AddReward` calls are left for the subclass of `Janelia.EasyMLAgentGrounded`.

#### 2H: Override `void Heuristic(in ActionBuffers actionsOut)`

A typical use of this method is to allow keyboard control of the agent for manual testing before automated training.  `Janelia.EasyMLAgentGrounded.Heuristic`, for example, uses `Input.GetKey(KeyCode.UpArrow)`, etc, to get the user's changes to forward movement and yaw rotation, and passes them to the agent as follows:

```
    Debug.Assert(VectorActionSize == 2, "Incorrect vector action size");

    ActionSegment<float>  continuouActions = actionsOut.ContinuousActions;
    continuouActions[0] = moveChange;
    continuouActions[1] = yawChange;
```
The number of action components should match the overridden `VectorActionSize` property value, with a `Vector3` being decomposed into three components.

#### 2I: Override `void CollectObservations(VectorSensor sensor)`

This method records observations of the environment deemed relevant for training.  Each observation is recorded with `sensor.AddObservation(X)`, and a variety of types for `X` are supported (e.g., `float`, `Vector3`, etc.).  The number of observations added should match the overridden `VectorObservationSize` property value, and note that a `Vector3` passed to `AddObservation` counts as three observations.

#### 2J: Consider Tuning the Child Sensor

By default, the agent has one "child sensor" that 
automatically adds observations based on what scene objects are hit by rays cast in front of the of the object.  Optionally, the agent subclass can override some properties to tune the details of this sensor:

* `bool UseChildSensorForward`: Set it to `false` to disable the child sensor.

* `List<string> ChildSensorForwardDetectableTags`: Set it to an array of strings, indicating the [tags](https://docs.unity3d.com/Manual/Tags.html) of the objects to be detected.  The default is `["Untagged"]`.

* `int ChildSensorForwardRaysPerDirection`: Set it to the number of rays to cast.

* `float ChildSensorForwardRayLength`: Set it to the maximum ray length.

#### 2K: Consider Adding `OnCollisionEnter(Collision collision)`

If a goal is to train the agent to move without hitting obstacles in the environment, then `OnCollisionEnter` is a good place to add a penalty (i.e., a `AddReward` with a negative value).  If only collisions with some objects are relevant, consider giving those objects a [tag](https://docs.unity3d.com/Manual/Tags.html), `T`, and checking for them with `collision.collider.CompareTag(T)`.

### 3: Create Using the Menu

Once subclasses of `Janelia.EasyMLArena` and `Janelia.EasyMLAgent` are defined in the project, the Unity editor's "GameObject" menu automatically gets the item "Create Easy ML Arena and Agent".  This menu item is defined in `EasyMLSetup.cs` but no changes to that code are necessary to make it work with new subclasses.  Simply choose the menu item to add the arena and agent to the scene.  To create additional arenas and agents for training with [multiple areas](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Learning-Environment-Design.md#multiple-areas), simply use the editor's "Edit/Duplicate" menu item and then change the top-level arena's position to avoid overlap.

### 4: Train

Use the standard ML-Agents Python script, `mlagents-learn`, to start training.  Then press "Play" in the Unity editor.  (There seems to be no need to build a standalone executable and leave the editor.)  If the agents are selected in the editor, and the editor is showing the "scene view" instead of the "game view", then the rays cast by their child sensors will be visible as training proceeds.

### 5: Refine

If training indicates that the code needs to be refined, the next round of training can begin immediately after changes to the following methods: `PlaceRandomly`, `OnActionReceived`, `Heuristic`, `CollectObservations`, and `OnCollisionEnter`.  Changes to the `Setup` methods or the [properties](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties) mentioned above take effect only after the "GameObject/Create Easy ML Arena and Agent" menu item is chosen again.

## Testing

To run this package's unit tests, use the following steps:
1. Create a new Unity project and add this package.
2. In the directory for the new project, in its `Packages` subdirectory, edit the `manifest.json` file to add a `"testables"` section as follows:
    ```
    {
      "dependencies": {
       ...
      },
      "testables": ["org.janelia.easy-ml-agents"]
    }
    ```
    Note the comma separating the `"dependencies"` and `"testables"` sections.
3. In the Unity editor's "Window" menu, under "General", choose "Test Runner".
4. In the new "Test Runner" window, the "EditMode" tab is selected by default.
5. There should be an item for the new project, with items underneath it for "Janelia.Easy-ml-agents.EditorTests.dll", etc.
6. Press the "Run All" button.
7. All the items under the new project will have green check marks if the tests succeed.
8. Next, choose the "PlayMode" tab.
9. There should be an item for the new project, with items underneath it for "Janelia.Easy-ml-agents.RuntimeTests.dll", etc.
10. Press the "Run All" button.
11. All the items under the new project will have green check marks if the tests succeed.
