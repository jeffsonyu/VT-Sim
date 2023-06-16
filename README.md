VT-Sim
===
This repository contains the Unity project of our **VT-Sim** for the paper [**Visual-Tactile Sensing for In-Hand Object Reconstruction**](https://sites.google.com/view/vtaco)

Other resources have been in the release.

## Installation
In this simulation environment, we use both **Python** and **Unity** to cooperate to produce simulation dataset.

### Python
We use **Python >= 3.7** to run the simulation environment. We recommend using **Anaconda** to manage the environment.
```
conda create -n vt-sim python=3.7
conda activate vt-sim
pip install -r requirements.txt
```

The python script **data_generate.py** is in the release, along with the hand urdf for loading in pybullet, the AKB models we use, and the Graspit! data we have generated. You can download them and put them in your own directory, and change their paths in the script.

### Unity
You can clone this repository and open the project in Unity. We use **Unity 2021.3.15f1c1** to develop this project. You can download it from [here](https://unity3d.com/get-unity/download/archive). We also use the [Obi SoftBody](https://assetstore.unity.com/packages/tools/physics/obi-softbody-130029) plugin in our Unity project, which you need to purchase and import into your project.

The path of mano_skin model is *Assets/Mano/mano_skin.prefab*

The models we use in the simulation are in the release. You can download them and unzip them into your Unity project. The models should be put into *Assets/Model*

The scene we use in the simulation is in *Assets/Scenes*, named **Mano** (Mano.unity)

## Usage and Instructions
**Note**: The RFUniverse API used in the following process can be found in the [pyrfuniverse documentation](https://mvig-robotflow.github.io/pyrfuniverse/pyrfuniverse.envs.html)

First change the paths in the python script **data_generate.py** to your own paths, and run the script.

---

The script starts the Unity simulation environment, passes in the parameters assets for the object model and hand model names, and preload.

```
env = RFUniverseBaseEnv(assets=['{}{}'.format(obj_class, obj_id), 'mano_skin'])
```

The code will hang after this step and you need to run **Mano** scene in UnityEditor manually.

---

The script creates an object model in the simulation environment and set Pose

```
obj_unity = env.InstanceObject(
                id=int(id_list[obj_class]+obj_id),
                name='{}{}'.format(obj_class, obj_id)
            )

obj_unity.SetTransform(
                position=[-obj_pos[1], obj_pos[2], obj_pos[0]],
                rotation=[-90, 0, 90]
            )
```

---

The script creates the hand model.

```
hand_unity = env.InstanceObject(
                id=12345678,
                name='mano_skin',
                attr_type=attr.ControllerAttr
            )
...
hand_unity.SetJointPosition(
            joint_positions=[end_info_1dof[i] * 57.29578 for i in range(15)]
        )
```

---

Get the camera instance according to the camera ID pre-placed in the scene and reset its Pose

```
camera1 = env.GetAttr(11111)
...
camera14 = env.GetAttr(111114)

camera1.SetTransform(
    position=[-obj_pos[1], obj_pos[2]+0.14, obj_pos[0]-0.5],
    rotation=[0, 0, 0]
)
...
camera14.SetTransform(
    position=[-obj_pos[1]+0.27, obj_pos[2]-0.1, obj_pos[0]+0.27],
    rotation=[-30, -135, 0]
)
env.step()
```

After screenshot the initial state of the scene, the script begin to first simulate the touch process in pybulelt, by slowly moving every finger to the target pose. As soon as pybullet detects contacts for every finger, the script will pass the hand wrist pose and finger joint parameters to Unity.

```
hand_unity.SetTransform(
                    position=[-basePosition_rot[1], basePosition_rot[2], basePosition_rot[0]],
                    rotation=[math.degrees(hand_eul[1]), -math.degrees(hand_eul[2]), -math.degrees(hand_eul[0])]
                )
...
hand_unity.SetJointPosition(
                    joint_positions=[end_info_1dof[i] * 57.29578 for i in range(15)]
                )
```


---

Save the current state of the camera image, object model mesh data and hand model mesh data

```
env.SendMessage('SaveData')
env.step()

env.SendMessage('SaveObjMesh')
env.step()

env.SendMessage('SaveMesh')
env.step()
```

---

The raw data will be saved in **save_root_dir**, in the following format:

```
dataset
├── 20231231235958
│   │── touch0001
|   |   │── Depth0.png
|   |   │── Light0.png
|   |   │── Occluded_depth001.png
|   |   │── ...
│   │── touch0001
│   │── ...
├── 20231231235959
├── ...
```
---
The data preprocess method will be released soon!