<div>

# Udon Noclip [![GitHub](https://img.shields.io/github/license/izykitten/VUdon-Noclip?color=blue&label=License&style=flat)](https://github.com/izykitten/VUdon-Noclip/blob/main/LICENSE) [![GitHub Repo stars](https://img.shields.io/github/stars/izykitten/VUdon-Noclip?style=flat&label=Stars)](https://github.com/izykitten/VUdon-Noclip/stargazers) [![GitHub all releases](https://img.shields.io/github/downloads/izykitten/VUdon-Noclip/total?color=blue&label=Downloads&style=flat)](https://github.com/izykitten/VUdon-Noclip/releases) [![GitHub tag (latest SemVer)](https://img.shields.io/github/v/tag/izykitten/VUdon-Noclip?color=blue&label=Release&sort=semver&style=flat)](https://github.com/izykitten/VUdon-Noclip/releases/latest)

</div>

Noclip prefab for VRChat worlds that allows players to fly through walls and objects, similar to noclip mode in Source games.

## Features

- Fly freely through any objects and walls
- Multiple activation methods: double jump, five jumps, or API-only
- Full VR and Desktop support with customizable controls
- Adjustable speed settings
- User restriction capabilities
- Automatic deactivation on respawn

## How To Use

1. Drag and drop the `Noclip` prefab `Noclip` into your scene.
2. Configure the settings in the Inspector to your preference.
3. Players can toggle noclip based on your chosen activation method.

> **NOTE:** To prevent users from activating noclip, **_DISABLE THE COMPONENT!_**

## Controls

### Activation Methods
- **Double Jump:** Quickly press jump twice to toggle noclip.
- **Five Jumps:** Press jump five times in quick succession to toggle noclip.
- **API Only:** Noclip can only be activated through UdonSharp API calls.

### Movement Controls
#### VR
- **Movement:** Use your regular VR movement controls.
- **Vertical:** Look up or down while moving to ascend/descend.

#### Desktop
- **WASD:** Forward/Left/Back/Right movement.
- **Space:** Ascend (default).
- **Left Shift:** Descend (default).
- **Left Control:** Speed boost (when held).

## Configuration Options

### Basic Settings
- **Noclip Trigger Method:** Choose how players activate noclip (Double Jump, Five Jumps, or API Only).
- **Toggle Threshold:** Time window for detecting double jumps or consecutive jumps (0.1s to 5s).
- **Speed:** Maximum movement speed in m/s (1-50).

### VR Settings
- **VR Input Multiplier:** Curve that maps VR movement input to speed multiplier.

### Desktop Settings
- **Desktop Speed Fraction:** Speed multiplier when not holding Shift.
- **Desktop Vertical Input:** Enable/disable vertical movement on desktop.
- **Up/Down Keys:** Customize keys for ascending/descending.

### User Restrictions
- **Restrict to Specific Users:** Enable to only allow listed users to use noclip.
- **Allowed Usernames:** List of users who can activate noclip when restrictions are enabled.

## API Methods

The noclip system provides several UdonSharp API methods:

```csharp
// Enable/disable noclip
_EnableNoclip();
_DisableNoclip();
_SetNoclipEnabled(bool enabled);

// Configure settings
_SetMaxSpeed(float maxSpeed);

// User management
_AddAllowedUser(string username);
_RemoveAllowedUser(string username);
_AddSelfToAllowedList();
_ClearAllowedUsers();
_SetUserRestrictions(bool restrict);
```

# Installation

### Using VRChat Creator Companion

1. Open the [VRChat Creator Companion](https://vcc.docs.vrchat.com/).
2. Navigate to the `Settings` tab.
3. Add the following repository URL under `User Repositories`:  
   `https://vpm.izy.sh/`
4. Go to the `Projects` tab and open your project.
5. In the `Packages` tab, search for `Udon Noclip`.
6. Click `Add` to include the package in your project.

<div align="center">

## Modified by izy, originally developed by Varneon with :hearts:

[![GitHub](https://img.shields.io/github/followers/izy?color=%23303030&label=izy&logo=GitHub&style=for-the-badge)](https://github.com/izy)

</div>
