using System.ComponentModel.DataAnnotations;
using TournamentTool.Attributes;
using TournamentTool.Models.Ranking;

namespace TournamentTool.Enums;

public enum MinecraftAdvancement
{
    [Display(Name = "None", Description = "none", ShortName = "none"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.All)] None,
    
    
    // --------------------------------
    // ========== Paceman ============
    // --------------------------------
    //, EnumRuleContext(ControllerMode.None, LeaderboardRuleType.None)
    [Display(Name = "(Paceman) Obtain iron ingot", Description = "rsg.obtain_iron_ingot", ShortName = "obtain_iron_ingot"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PaceObtainIronIngot,
    [Display(Name = "(Paceman) Obtain iron pickaxe", Description = "rsg.obtain_iron_pickaxe", ShortName = "obtain_iron_pickaxe"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanObtainIronPickaxe,
    [Display(Name = "(Paceman) Obtain lava bucket", Description = "rsg.obtain_lava_bucket", ShortName = "obtain_iron_bucket"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanObtainLavaBucket,
    [Display(Name = "(Paceman) Obtain Obsidian", Description = "rsg.obtain_obsidian", ShortName = "obtain_obsidian"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanObtainObsidian,
    [Display(Name = "(Paceman) Enter nether", Description = "rsg.enter_nether", ShortName = "enter_nether"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanEnterNether,
    [Display(Name = "(Paceman) Enter bastion", Description = "rsg.enter_bastion", ShortName = "enter_bastion"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanEnterBastion,
    [Display(Name = "(Paceman) Enter fortress", Description = "rsg.enter_fortress", ShortName = "enter_fortress"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanEnterFortress,
    [Display(Name = "(Paceman) First portal", Description = "rsg.first_portal", ShortName = "first_portal"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanFirstPortal,
    [Display(Name = "(Paceman) Second portal", Description = "rsg.second_portal", ShortName = "second_portal"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanSecondPortal,
    [Display(Name = "(Paceman) Enter stronghold", Description = "rsg.enter_stronghold", ShortName = "enter_stronghold"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanEnterStronghold,
    [Display(Name = "(Paceman) Enter end", Description = "rsg.enter_end", ShortName = "enter_end"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanEnterEnd,
    [Display(Name = "(Paceman) Credits", Description = "rsg.credits", ShortName = "credits"), EnumRuleContext(ControllerMode.Paceman, LeaderboardRuleType.Split)] PacemanCredits,
    
    // --------------------------------
    // ========== Ranked ============
    // --------------------------------
    
    [Display(Name = "(Ranked) Blind Travel", Description = "projectelo.timeline.blind_travel", ShortName = "blind_travel"), EnumRuleContext(ControllerMode.Ranked, LeaderboardRuleType.Split)] ProjectEloBlindTravel,
    [Display(Name = "(Ranked) Complete", Description = "projectelo.timeline.complete", ShortName = "complete"), EnumRuleContext(ControllerMode.Ranked, LeaderboardRuleType.Split)] ProjectEloComplete,
    
    [Display(Name = "(Ranked) Dragon Death", Description = "projectelo.timeline.dragon_death", ShortName = "dragon_death"), EnumRuleContext(ControllerMode.Ranked, LeaderboardRuleType.Advancement)] ProjectEloDragonDeath,
    [Display(Name = "(Ranked) Forfeit", Description = "projectelo.timeline.forfeit", ShortName = "forfeit"), EnumRuleContext(ControllerMode.Ranked, LeaderboardRuleType.Advancement)] ProjectEloForfeit,
    [Display(Name = "(Ranked) Death", Description = "projectelo.timeline.death", ShortName = "death"), EnumRuleContext(ControllerMode.Ranked, LeaderboardRuleType.Advancement)] ProjectEloDeath,
    
    // --------------------------------
    // ========== Advancements ============
    // --------------------------------
    
    // ====== Story Advancements ======
    [Display(Name = "Minecraft", Description = "story.root", ShortName = "story"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryRoot,
    [Display(Name = "Stone Age", Description = "story.mine_stone", ShortName = "mine_stone"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryMineStone,
    [Display(Name = "Getting an Upgrade", Description = "story.upgrade_tools", ShortName = "upgrade_tools"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryUpgradeTools,
    [Display(Name = "Acquire Hardware", Description = "story.smelt_iron", ShortName = "smelt_iron"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StorySmeltIron,
    [Display(Name = "Suit Up", Description = "story.obtain_armor", ShortName = "obtain_armor"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryObtainArmor,
    [Display(Name = "Hot Stuff", Description = "story.lava_bucket", ShortName = "lava_bucket"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryLavaBucket,
    [Display(Name = "Isn't It Iron Pick", Description = "story.iron_tools", ShortName = "iron_tools"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryIronTools,
    [Display(Name = "Not Today, Thank You", Description = "story.deflect_arrow", ShortName = "deflect_arrow"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryDeflectArrow,
    [Display(Name = "Ice Bucket Challenge", Description = "story.form_obsidian", ShortName = "form_obsidian"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryFormObsidian,
    [Display(Name = "Diamonds!", Description = "story.mine_diamond", ShortName = "mine_diamond"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryMineDiamond,
    [Display(Name = "We Need to Go Deeper", Description = "story.enter_the_nether", ShortName = "enter_the_nether"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryEnterTheNether,
    [Display(Name = "Cover Me with Diamonds", Description = "story.shiny_gear", ShortName = "shiny_gear"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryShinyGear,
    [Display(Name = "Enchanter", Description = "story.enchant_item", ShortName = "enchant_item"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryEnchantItem,
    [Display(Name = "Zombie Doctor", Description = "story.cure_zombie_villager", ShortName = "cure_zombie_villager"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryCureZombieVillager,
    [Display(Name = "Eye Spy", Description = "story.follow_ender_eye", ShortName = "follow_ender_eye"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryFollowEnderEye,
    [Display(Name = "The End?", Description = "story.enter_the_end", ShortName = "enter_the_end"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] StoryEnterTheEnd,

    // ====== Nether Advancements ======
    [Display(Name = "Nether", Description = "nether.root", ShortName = "nether"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherRoot,
    [Display(Name = "Return to Sender", Description = "nether.return_to_sender", ShortName = "return_to_sender"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherReturnToSender,
    [Display(Name = "Those Were the Days", Description = "nether.find_bastion", ShortName = "find_bastion"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherFindBastion,
    [Display(Name = "Hidden in the Depths", Description = "nether.obtain_ancient_debris", ShortName = "obtain_ancient_debris"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherObtainAncientDebris,
    [Display(Name = "Subspace Bubble", Description = "nether.fast_travel", ShortName = "fast_travel"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherFastTravel,
    [Display(Name = "A Terrible Fortress", Description = "nether.find_fortress", ShortName = "find_fortress"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherFindFortress,
    [Display(Name = "Who is Cutting Onions?", Description = "nether.obtain_crying_obsidian", ShortName = "obtain_crying_obsidian"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherObtainCryingObsidian,
    [Display(Name = "Oh Shiny", Description = "nether.distract_piglin", ShortName = "distract_piglin"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherDistractPiglin,
    [Display(Name = "This Boat Has Legs", Description = "nether.ride_strider", ShortName = "ride_strider"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherRideStrider,
    [Display(Name = "Uneasy Alliance", Description = "nether.uneasy_alliance", ShortName = "uneasy_alliance"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherUneasyAlliance,
    [Display(Name = "War Pigs", Description = "nether.loot_bastion", ShortName = "loot_bastion"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherLootBastion,
    [Display(Name = "Country Lode, Take Me Home", Description = "nether.use_lodestone", ShortName = "use_lodestone"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherUseLodestone,
    [Display(Name = "Cover Me in Debris", Description = "nether.netherite_armor", ShortName = "netherite_armor"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherNetheriteArmor,
    [Display(Name = "Spooky Scary Skeleton", Description = "nether.get_wither_skull", ShortName = "get_wither_skull"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherGetWitcherSkull,
    [Display(Name = "Into Fire", Description = "nether.obtain_blaze_rod", ShortName = "obtain_blaze_rod"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherObtainBlazeRod,
    [Display(Name = "Not Quite \"Nine\" Lives", Description = "nether.charge_respawn_anchor", ShortName = "charge_respawn_anchor"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherChargeRespawnAnchor,
    [Display(Name = "Feels Like Home", Description = "nether.ride_strider_in_overworld_lava", ShortName = "ride_strider_in_overworld_lava"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherRiderStriderInOverworldLava,
    [Display(Name = "Hot Tourist Destinations", Description = "nether.explore_nether", ShortName = "explore_nether"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherExploreNether,
    [Display(Name = "Withering Heights", Description = "nether.summon_wither", ShortName = "summon_wither"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherSummonWither,
    [Display(Name = "Local Brewery", Description = "nether.brew_potion", ShortName = "brew_potion"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherBrewPotion,
    [Display(Name = "Bring Home the Beacon", Description = "nether.create_beacon", ShortName = "create_beacon"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherCreateBeacon,
    [Display(Name = "A Furious Cocktail", Description = "nether.all_potions", ShortName = "all_potions"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherAllPotions,
    [Display(Name = "Beaconator", Description = "nether.create_full_beacon", ShortName = "create_full_beacon"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherCreateFullBeacon,
    [Display(Name = "How Did We Get Here?", Description = "nether.all_effects", ShortName = "all_effects"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] NetherAllEffects,

    // ====== The End Advancements ======
    [Display(Name = "The End", Description = "end.root", ShortName = "end"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] EndRoot,
    [Display(Name = "Free the End", Description = "end.kill_dragon", ShortName = "kill_dragon"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] EndKillDragon,
    [Display(Name = "The Next Generation", Description = "end.dragon_egg", ShortName = "dragon_egg"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] EndDragonEgg,
    [Display(Name = "Remote Getaway", Description = "end.enter_end_gateway", ShortName = "enter_end_gateway"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] EndEnterEndGateway,
    [Display(Name = "The End... Again...", Description = "end.respawn_dragon", ShortName = "respawn_dragon"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] EndRespawnDragon,
    [Display(Name = "You Need a Mint", Description = "end.dragon_breath", ShortName = "dragon_breath"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] EndDragonBreath,
    [Display(Name = "The City at the End of the Game", Description = "end.find_end_city", ShortName = "find_end_city"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] EndFindEndCity,
    [Display(Name = "Sky's the Limit", Description = "end.elytra", ShortName = "elytra"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] EndElytra,
    [Display(Name = "Great View From Up Here", Description = "end.levitate", ShortName = "levitate"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] EndLevitate,

    // ====== Adventure Advancements =======
    [Display(Name = "Adventure", Description = "adventure.root", ShortName = "adventure"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureRoot,
    [Display(Name = "Voluntary Exile", Description = "adventure.voluntary_exile", ShortName = "voluntary_exile"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureVoluntaryExile,
    [Display(Name = "Is It a Bird?", Description = "adventure.spyglass_at_parrot", ShortName = "spyglass_at_parrot"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureSpyglassAtParrot,
    [Display(Name = "Monster Hunter", Description = "adventure.kill_a_mob", ShortName = "kill_a_mob"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureKillAMob,
    [Display(Name = "The Power of Books", Description = "adventure.read_power_of_chiseled_bookshelf", ShortName = "read_power_of_chiseled_bookshelf"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureReadPowerOfChiseledBookshelf,
    [Display(Name = "What a Deal!", Description = "adventure.trade", ShortName = "trade"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureTrade,
    [Display(Name = "Crafting a New Look", Description = "adventure.trim_with_any_armor_pattern", ShortName = "trim_with_any_armor_pattern"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureTrimAnyArmor,
    [Display(Name = "Sticky Situation", Description = "adventure.honey_block_slide", ShortName = "honey_block_slide"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureHoneyBlockSlide,
    [Display(Name = "Ol' Betsy", Description = "adventure.ol_betsy", ShortName = "ol_betsy"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureOlBetsy,
    [Display(Name = "Surge Protector", Description = "adventure.lightning_rod_with_villager_no_fire", ShortName = "lightning_rod_with_villager_no_fire"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureLightningRodWithVillagerNoFire,
    [Display(Name = "Caves & Cliffs", Description = "adventure.fall_from_world_height", ShortName = "fall_from_world_height"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureFallFromWorldHeight,
    [Display(Name = "Respecting the Remnants", Description = "adventure.salvage_sherd", ShortName = "salvage_sherd"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureSalvageSherd,
    [Display(Name = "Sneak 100", Description = "adventure.avoid_vibration", ShortName = "avoid_vibration"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureAvoidVibration,
    [Display(Name = "Sweet Dreams", Description = "adventure.sleep_in_bed", ShortName = "sleep_in_bed"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureSleepInBed,
    [Display(Name = "Hero of the Village", Description = "adventure.hero_of_the_village", ShortName = "hero_of_the_village"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureHeroOfTheVillage,
    [Display(Name = "Is It a Balloon?", Description = "adventure.spyglass_at_ghast", ShortName = "spyglass_at_ghast"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureSpyglassAtGhast,
    [Display(Name = "A Throwaway Joke", Description = "adventure.throw_trident", ShortName = "throw_trident"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureThrowTrident,
    [Display(Name = "It Spreads", Description = "adventure.kill_mob_near_sculk_catalyst", ShortName = "kill_mob_near_sculk_catalyst"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureKillMobNearSculkCatalyst,
    [Display(Name = "Take Aim", Description = "adventure.shoot_arrow", ShortName = "shoot_arrow"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureShootArrow,
    [Display(Name = "Monsters Hunted", Description = "adventure.kill_all_mobs", ShortName = "kill_all_mobs"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureKillAllMobs,
    [Display(Name = "Postmortal", Description = "adventure.totem_of_undying", ShortName = "totem_of_undying"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureTotemOfUndying,
    [Display(Name = "Hired Help", Description = "adventure.summon_iron_golem", ShortName = "summon_iron_golem"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureSummonIronGolem,
    [Display(Name = "Star Trader", Description = "adventure.trade_at_world_height", ShortName = "trade_at_world_height"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureTradeAtWorldHeight,
    [Display(Name = "Smithing with Style", Description = "adventure.trim_with_all_exclusive_armor_patterns", ShortName = "trim_with_all_exclusive_armor_patterns"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureTrimWithAllExclusiveArmorPatterns,
    [Display(Name = "Two Birds, One Arrow", Description = "adventure.two_birds_one_arrow", ShortName = "two_birds_one_arrow"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureTwoBirdsOneArrow,
    [Display(Name = "Who's the Pillager Now?", Description = "adventure.whos_the_pillager_now", ShortName = "whos_the_pillager_now"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureWhoThePillagerNow,
    [Display(Name = "Arbalistic", Description = "adventure.arbalistic", ShortName = "arbalistic"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureArbalistic,
    [Display(Name = "Careful Restoration", Description = "adventure.craft_decorated_pot_using_only_sherds", ShortName = "craft_decorated_pot_using_only_sherds"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureCraftDecoratedPotUsingOnlySherds,
    [Display(Name = "Adventuring Time", Description = "adventure.adventuring_time", ShortName = "adventuring_time"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureAdventuringTime,
    [Display(Name = "Sound of Music", Description = "adventure.play_jukebox_in_meadows", ShortName = "play_jukebox_in_meadows"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventurePlayJukeboxInMeadows,
    [Display(Name = "Light as a Rabbit", Description = "adventure.walk_on_powder_snow_with_leather_boots", ShortName = "walk_on_powder_snow_with_leather_boots"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureWalOnPowderSnowWithLeatherBoots,
    [Display(Name = "Is It a Plane?", Description = "adventure.spyglass_at_dragon", ShortName = "spyglass_at_dragon"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureSpyglassAtDragon,
    [Display(Name = "Very Very Frightening", Description = "adventure.very_very_frightening", ShortName = "very_very_frightening"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureVeryVeryFrightening,
    [Display(Name = "Sniper Duel", Description = "adventure.sniper_duel", ShortName = "sniper_duel"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureSniperDuel,
    [Display(Name = "Bullseye", Description = "adventure.bullseye", ShortName = "bullseye"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] AdventureBullseye,

    // ====== Husbandry Advancements ======
    [Display(Name = "Husbandry", Description = "husbandry.root", ShortName = "husbandry"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryRoot,
    [Display(Name = "Bee Our Guest", Description = "husbandry.safely_harvest_honey", ShortName = "safely_harvest_honey"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandrySafelyHarvestHoney,
    [Display(Name = "The Parrots and the Bats", Description = "husbandry.breed_an_animal", ShortName = "breed_an_animal"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryBreedAnAnimal,
    [Display(Name = "You've Got a Friend in Me", Description = "husbandry.allay_deliver_item_to_player", ShortName = "allay_deliver_item_to_player"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryAllayDeliverItemToPlayer,
    [Display(Name = "Whatever Floats Your Goat!", Description = "husbandry.ride_a_boat_with_a_goat", ShortName = "ride_a_boat_with_a_goat"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryRideABoatWithAGoat,
    [Display(Name = "Best Friends Forever", Description = "husbandry.tame_an_animal", ShortName = "tame_an_animal"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryTameAnAnimal,
    [Display(Name = "Glow and Behold!", Description = "husbandry.make_a_sign_glow", ShortName = "make_a_sign_glow"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryMakeASignGlow,
    [Display(Name = "Fishy Business", Description = "husbandry.fishy_business", ShortName = "fishy_business"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryFishyBusiness,
    [Display(Name = "Total Beelocation", Description = "husbandry.silk_touch_nest", ShortName = "silk_touch_nest"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandrySilkTouchNest,
    [Display(Name = "Bukkit Bukkit", Description = "husbandry.tadpole_in_a_bucket", ShortName = "tadpole_in_a_bucket"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryTadpoleInABucket,
    [Display(Name = "Smells Interesting", Description = "husbandry.obtain_sniffer_egg", ShortName = "obtain_sniffer_egg"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryObtainSnifferEgg,
    [Display(Name = "A Seedy Place", Description = "husbandry.plant_seed", ShortName = "plant_seed"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryPlantSeed,
    [Display(Name = "Wax On", Description = "husbandry.wax_on", ShortName = "wax_on"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryWaxOn,
    [Display(Name = "Two by Two", Description = "husbandry.bred_all_animals", ShortName = "bred_all_animals"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryBredAllAnimals,
    [Display(Name = "Birthday Song", Description = "husbandry.allay_deliver_cake_to_note_block", ShortName = "allay_deliver_cake_to_note_block"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryAllayDeliverCakeToNoteBlock,
    [Display(Name = "A Complete Catalogue", Description = "husbandry.complete_catalogue", ShortName = "complete_catalogue"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryCompleteCatalogue,
    [Display(Name = "Tactical Fishing", Description = "husbandry.tactical_fishing", ShortName = "tactical_fishing"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryTacticalFishing,
    [Display(Name = "When the Squad Hops into Town", Description = "husbandry.leash_all_frog_variants", ShortName = "leash_all_frog_variants"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryLeashAllFrogVariants,
    [Display(Name = "Little Sniffs", Description = "husbandry.feed_snifflet", ShortName = "feed_snifflet"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryFeedSnifflet,
    [Display(Name = "A Balanced Diet", Description = "husbandry.balanced_diet", ShortName = "balanced_diet"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryBalancedDiet,
    [Display(Name = "Serious Dedication", Description = "husbandry.obtain_netherite_hoe", ShortName = "obtain_netherite_hoe"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryObtainNetheriteHoe,
    [Display(Name = "Wax Off", Description = "husbandry.wax_off", ShortName = "wax_off"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryWaxOff,
    [Display(Name = "The Cutest Predator", Description = "husbandry.axolotl_in_a_bucket", ShortName = "axolotl_in_a_bucket"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryAxolotlInABucket,
    [Display(Name = "With Our Powers Combined!", Description = "husbandry.froglights", ShortName = "froglights"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryFroglights,
    [Display(Name = "Planting the Past", Description = "husbandry.plant_any_sniffer_seed", ShortName = "plant_any_sniffer_seed"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryPlantAnySnifferSeed,
    [Display(Name = "The Healing Power of Friendship!", Description = "husbandry.kill_axolotl_target", ShortName = "kill_axolotl_target"), EnumRuleContext(ControllerMode.All, LeaderboardRuleType.Advancement)] HusbandryKillAxolotlTarget,
}