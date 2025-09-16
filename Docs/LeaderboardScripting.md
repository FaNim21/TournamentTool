# Lua API Documentation for Tournament Tool

## Table of Contents
- [Introduction](#introduction)
- [Getting Started](#getting-started)
- [Script Structure](#script-structure)
- [Rules Edit Window Overview](#rules-edit-window-overview)
- [API Overview](#api-overview)
- [Variable Calls](#variable-calls)
    - [General Variables](#general-variables)
    - [Paceman Mode Variables](#paceman-mode-variables)
    - [Ranked Mode Variables](#ranked-mode-variables)
    - [Player Data Object](#player-data-object)
- [Method Calls](#method-calls)
- [Global Method Calls](#global-method-calls)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)

## Introduction
This documentation covers the Lua API for the Tournament Tool. The API allows you to create custom scoring rules for leaderboard using Lua scripts.

## Getting Started
If you're new to Lua, we recommend:
- [Learn Lua in Y minutes](https://learnxinyminutes.com/docs/lua/) - Quick syntax overview
- [Lua 5.4 Reference Manual](https://www.lua.org/manual/5.4/) - Official documentation

### Best Practices For Leaderboard Scripts
1. Always include script metadata (`version`, `type`, `description`)
2. Register variables at the global scope (outside functions)
3. Use the `api` object to access all leaderboard API data
4. Remember that Lua arrays are 1-indexed
5. Use `print()` or `error()` statements for debugging during script development

### Script Structure
Every leaderboard script must follow this structure:

```lua
-- Script metadata
version = "1.0.0" -- Semantic versioning
type = "normal" -- or "ranked"
description = "Description of what this script does, along with an explanation of the registered custom variables"

-- Register custom variables (optional)
register_variable("variable_name", "type", default_value)

-- Main evaluation function (required)
function evaluate_data(api)
    -- Your scoring logic here
end
```

## Rules Edit Window Overview
[//]: # (![Whitelist panel view]&#40;Images/Whitelist.png&#41;)

<p align="center">
  <img src="Images/Rule-edit-window.png" alt="Whitelist panel view">
</p>
<br>
<p align="center">
  <img src="Images/SubRule-edit-window.png" alt="Whitelist panel view">
</p>

## API Overview
The Tournament Tool Lua API provides access to tournament data through an `api` object passed to the `evaluate_data` function.
The API behaves differently depending on the controller mode set in preset:
- **Normal/Paceman Mode**: Individual player scoring splits in Random Seed Glitchless category based on [paceman](https://paceman.gg/) live data
- **Ranked Mode**: Multiple player competing in [ranked](https://mcsrranked.com/) private room based on MCSR Ranked live match data api

## Variable Calls
All variables are accessed through the `api` object within the `evaluate_data` function.

### General Variables
These variables are available in both Normal and Ranked modes:

#### `api.rule_name`
- **Type**: string (read-only)
- **Description**: Returns the name of the rule as configured in the rule editor
- **Example**: `local name = api.rule_name`

#### `api.rule_type`
- **Type**: string (read-only)
- **Description**: Returns the rule type ("split" or "advancement")
- **Example**: `local type = api.rule_type`

#### `api.milestone_name`
- **Type**: string (read-only)
- **Description**: Returns the milestone name from the rule editor. For advancement rules, returns only the last part of the full name
- **Example**: Minecraft Advancement `"story.enter_the_nether"` returns `"enter_the_nether"`

#### `api.base_points`
- **Type**: number (read-only)
- **Description**: Base points value configured in the rule editor
- **Example**: `local base = api.base_points`

### Paceman Mode Variables
These variables are only available in Normal/Paceman mode:

#### `api.player_position`
- **Type**: number (read-only)
- **Description**: Current position of the player on the leaderboard
- **Example**: `local pos = api.player_position`

#### `api.player_points`
- **Type**: number (read-only)
- **Description**: Total points accumulated by the player
- **Example**: `local pts = api.player_points`

#### `api.player_time`
- **Type**: number (read-only)
- **Description**: Time taken to complete the current milestone (in miliseconds)
- **Example**: `local time = api.player_time`

#### `api.player_milestone_best_time`
- **Type**: number (read-only)
- **Description**: Player's best time for the current milestone type (in miliseconds)
- **Example**: `local best = api.player_milestone_best_time`

#### `api.player_milestone_amount`
- **Type**: number (read-only)
- **Description**: Number of times the player has completed this milestone type
- **Example**: `local count = api.player_milestone_amount`

#### `api.player_name`
- **Type**: string (read-only)
- **Description**: Player's in-game name in the whitelist
- **Example**: `local name = api.player_name`

### Ranked Mode Variables
These variables are only available in Ranked mode:

#### `api.round`
- **Type**: number (read-only)
- **Description**: Current round number
- **Example**: `local current_round = api.round`

#### `api.max_winners`
- **Type**: number (read-only)
- **Description**: Maximum number of winners allowed per round (configured in sub rule editor)
- **Example**: `local winners = api.max_winners`

#### `api.players_in_round`
- **Type**: number (read-only)
- **Description**: Total number of active players in the current round (excludes spectators)
- **Example**: `local player_count = api.players_in_round`

#### `api.completions_in_round`
- **Type**: number (read-only)
- **Description**: Number of players who have completed the milestone in the current round
- **Example**: `local completed = api.completions_in_round`

#### `api.players`
- **Type**: array of PlayerData objects (read-only)
- **Description**: Array containing all players in the current round
- **Example**:
```lua
for i, player in ipairs(api.players) do
    -- Process each player
end
```

### Player Data Object
When iterating through the `api.players` array in Ranked mode, each player object contains:

#### `player.position`
- **Type**: number (read-only)
- **Description**: Player's current position on the leaderboard

#### `player.points`
- **Type**: number (read-only)
- **Description**: Player's total points on the leaderboard

#### `player.time`
- **Type**: number (read-only)
- **Description**: Player's completion time for the current milestone (in miliseconds)

#### `player.milestone_best_time`
- **Type**: number (read-only)
- **Description**: Player's best time for this milestone type (in miliseconds)

#### `player.milestone_amount`
- **Type**: number (read-only)
- **Description**: Number of times the player has completed this milestone type

#### `player.name`
- **Type**: string (read-only)
- **Description**: Player's in-game name in the whitelist

## Method Calls

### `api:register_milestone(points)` (Normal/Paceman Mode)
### `api:register_milestone(player, points)` (Ranked Mode)
- **Parameters**:
    - `points` (number): Points to award
    - `player` (PlayerData): Player object (Ranked mode only)
- **Returns**: void
- **Description**: Awards points to a player for completing a milestone
- **Example**:
```lua
-- Normal/Paceman mode
api:register_milestone(api.base_points)

-- Ranked mode
for i, player in ipairs(api.players) do
    api:register_milestone(player, api.base_points)
end
```

### `api:get_variable(name)`
- **Parameters**:
    - `name` (string): Variable name to retrieve
- **Returns**: Value of the variable (type depends on registration)
- **Description**: Retrieves the current value of a custom variable
- **Example**:
```lua
local message = api:get_variable("bonus_message")
local multiplier = api:get_variable("bonus_multiplier")
local enabled = api:get_variable("enable_bonus")
```

### `api:set_variable(name, value)`
- **Parameters**:
    - `name` (string): Variable name to update
    - `value`: New value (must match the variable's registered type)
- **Returns**: void
- **Description**: Updates the value of a custom variable
- **Example**:
```lua
api:set_variable("bonus_message", "Nice!")
api:set_variable("bonus_multiplier", 2.0)
api:set_variable("enable_bonus", false)
```

## Global Method Calls

### `register_variable(name, type, default_value)`
- **Scope**: Global (must be called outside the `evaluate_data` function)
- **Parameters**:
    - `name` (string): Variable identifier
    - `type` (string): "number", "string", or "bool"
    - `default_value`: Initial value (must match the specified type)
- **Returns**: void
- **Description**: Registers a custom variable that appears in the tournament tool's rule editor
- **Example**:
```lua
register_variable("bonus_message", "string", "Great job!")
register_variable("bonus_multiplier", "number", 1.5)
register_variable("enable_bonus", "bool", true)
```

### `print(message...)`
- **Scope**: Global (must be called outside the `evaluate_data` function)
- **Parameters**:
    - `message` (any): message value for logging purposes
- **Returns**: void
- **Description**: Logs message into tournament tool logging system (as for 0.12.0 there is no built-in console to view data)
- **Example**:
```lua
print("Succesfully evaluated!")
print("Base points = ", api.base_points)
print(i - 1, " - player: ", player.name, ", points: ", points)
```

### `error(message...)`
- **Scope**: Global (must be called outside the `evaluate_data` function)
- **Parameters**:
    - `message` (any): message value for error logging purposes
- **Returns**: void
- **Description**: Logs error message into tournament tool logging system visible in notification panel available through bell button in bottom status bar
- **Example**:
```lua
print("Succesfully evaluated!")
print("Base points = ", api.base_points)
print(i - 1, " - player: ", player.name, ", points: ", points)
```


## Complete Examples

### Normal/Paceman Mode Example
```lua
version = "1.0.0"
type = "normal"
description = "Basic script adding base point to successfully evaluated player.\ncount - displays amount of evaluated milestones\nlast_player - displays last evaluated player ign"

register_variable("count", "number", 0)
register_variable("last_player", "string", "none")

function evaluate_data(api)
    api:set_variable("count", api:get_variable("count") + 1)
    api:set_variable("last_player", api.player_name)

    api:register_milestone(api.base_points)
end
```

### Ranked Mode Example
```lua
version = "1.0.0"
type = "ranked"
description = "Basic Ranked type script adding base point to all evaluated players.\namount_players_evaluated - displays amount of players evaluated last round"

register_variable("amount_players_evaluated", "number", 0)

function evaluate_data(api)
    api:set_variable("amount_players_evaluated", #api.players)

    for _, player in ipairs(api.players) do
        api:register_milestone(player, api.base_points)
    end
end
```

### Ranked Mode Advanced Example
```lua
version = "2.0.0"
type = "ranked"
description = "Points evaluation based on showdown/lcq points distribution.\nplayers_amount - last number of players evaluated by the script"

register_variable("players_amount", "number", 0)

function evaluate_data(api)
    api:set_variable("playersAmount", #api.players)

    print("Evaluation for: ", api.milestone_name)
    print("==================================================================")
    for i, player in ipairs(api.players) do
        local points = round((#api.players - (i - 1)) / #api.players * api.base_points)
        print(i - 1, " - player: ", player.name, ", points: ", points)

        api:register_milestone(player, points)
    end
    print("==================================================================")
end

function round(x)
    return math.floor(x + 0.5)
end
```



# Testing markdown
| Syntax    | Description |
|-----------|-------------|
| Header    | Title       |
| Paragraph | Text        |

```lua
version = "1.0.0"
type = "normal"
description = "Basic script adding base point to successfully evaluated player.\ncount - displays amount of evaluated milestones\nlast_player - displays last evaluated player ign"

register_variable("count", "number", 0)
register_variable("last_player", "string", "none")

function evaluate_data(api)
    api:set_variable("count", api:get_variable("count") + 1)
    api:set_variable("last_player", api.player_name)

    api:register_milestone(api.base_points)
end
```

```lua
version = "1.0.0"
type = "normal"
description = "Basic script adding base point to successfully evaluated player.\ncount - displays amount of evaluated milestones\nlast_player - displays last evaluated player ign"

register_variable("count", "number", 0)
register_variable("last_player", "string", "none")

function evaluate_data(api)
    api:set_variable("count", api:get_variable("count") + 1)
    api:set_variable("last_player", api.player_name)

    api:register_milestone(api.base_points)
end
```

<a id="123"></a>
# elo
- sdf 