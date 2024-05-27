using OWML.Utils;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using OWML.ModHelper;

namespace LightBramble
{
	public static class Patches
	{
		public static void ApplyPatches() => Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
	}

	[HarmonyPatch]
	public static class AnglerPatches
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(SectoredMonoBehaviour), nameof(SectoredMonoBehaviour.OnSectorOccupantAdded))]
		public static void OnSectorOccupantAdded(SectoredMonoBehaviour __instance, SectorDetector sectorDetector)
		{
			if (__instance.GetType() != typeof(AnglerfishController))
				return;

			AnglerfishController anglerFishController = (AnglerfishController)__instance;

			//var sector = anglerFishController.GetValue<Sector>("_sector");

			LightBramble.inst.DebugLog("angler sector occupant added, sectorDetector is " + sectorDetector.gameObject.name + " , occupant type is " + sectorDetector.GetOccupantType());

			if ((sectorDetector.GetOccupantType() == DynamicOccupant.Player || sectorDetector.GetOccupantType() == DynamicOccupant.Probe))
			{
				LightBramble.inst.ToggleFishes(LightBramble.inst.DisableFishConfig);
				LightBramble.inst.DebugLog("toggling fish, DisableFishConfig is " + LightBramble.inst.DisableFishConfig);
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.OnSectorOccupantsUpdated))]
		public static bool OnSectorOccupantsUpdated(AnglerfishController __instance)
		{
			var sector = __instance.GetValue<Sector>("_sector");

			if (__instance.gameObject.activeSelf && !sector.ContainsAnyOccupants(DynamicOccupant.Player | DynamicOccupant.Probe | DynamicOccupant.Ship))
			{
				LightBramble.inst.DebugLog("player, probe, and ship all left sector. disabling fish");
				LightBramble.inst.ToggleFishes(shouldDisable: true);
			}
			return false;


			//LightBramble.inst.ModHelper.Events.Unity.FireInNUpdates(() =>
			//	LightBramble.inst.ToggleFishes(LightBramble.inst.DisableFishConfig), 2);

			//if (!__instance.gameObject.activeSelf && sector.ContainsAnyOccupants(DynamicOccupant.Player | DynamicOccupant.Probe | DynamicOccupant.Ship))
			//{
			//	__instance.gameObject.SetActive(true);
			//	__instance.GetAttachedOWRigidbody()?.Unsuspend(true);
			//	__instance.RaiseEvent("OnAnglerUnsuspended", currentState);
			//}
			//else if (__instance.gameObject.activeSelf && !sector.ContainsAnyOccupants(DynamicOccupant.Player | DynamicOccupant.Probe | DynamicOccupant.Ship))
			//{
			//	__instance.GetAttachedOWRigidbody()?.Suspend();
			//	__instance.gameObject.SetActive(false);
			//	__instance.RaiseEvent("OnAnglerSuspended", currentState);
			//}

			//return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.Awake))]
		public static void AwakePostfix(AnglerfishController __instance)
		{
			LightBramble.inst.collections.anglerfishList.Add(__instance);
		}
	}

	[HarmonyPatch]
	public static class FogLightManagerPatch
	{
		//[HarmonyPrefix]
		//[HarmonyPatch(typeof(FogLightManager), nameof(FogLightManager.WillRenderCanvases))]
		//public static bool WillRenderCanvasesPrefix(FogLightManager __instance)
		//{
		//	if (LightBramble.inst.DisableFishConfig)
		//		return false;

		//	return true;
		//}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(FogLightManager), "IsLightVisible")]
		public static bool IsLightVisiblePatch(ref bool __result)
		{
			//if disabling fish, then always return false to hide lights
			if (LightBramble.inst.DisableFishConfig)
			{
				__result = false;
				return false;	//do not run original
			}
			//if not disabling fish, then allow the function to run as normally
			else
				return true;
		}
	}

	[HarmonyPatch]
	public static class FogPatches
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(FogWarpVolume), nameof(FogWarpVolume.Awake))]
		public static void FogWarpVolumePostfix(FogWarpVolume __instance)
		{
			LightBramble.inst.collections.fogWarpVolumeDict.Add(__instance, __instance.GetValue<Color>("_fogColor"));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlanetaryFogController), nameof(PlanetaryFogController.Awake))]
		public static void PlanetaryFogPostfix(PlanetaryFogController __instance)
		{
			LightBramble.inst.collections.planetaryFogControllerDict.Add(__instance, __instance.fogTint);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(FogOverrideVolume), nameof(FogOverrideVolume.Awake))]
		public static void FogOverrideVolumePostfix(FogOverrideVolume __instance)
		{
			LightBramble.inst.collections.fogOverrideVolumeDict.Add(__instance, __instance.tint);
		}
	}

	[HarmonyPatch]
	public static class GlobalMusicControllerPatch
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(GlobalMusicController), nameof(GlobalMusicController.Start))]
		static public void GlobalMusicControllerStartPostfix(GlobalMusicController __instance)
		{
			LightBramble.inst.musicManager = new MusicManager(__instance);
		}
	}

	[HarmonyPatch]
	public static class AnglerfishAudioControllerPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(AnglerfishAudioController), nameof(AnglerfishAudioController.UpdateLoopingAudio))]
		public static void UpdateLoopingAudioPatch(AnglerfishAudioController __instance, ref bool __runOriginal, AnglerfishController.AnglerState anglerState)
		{
			__runOriginal = false;
			LightBramble.inst.DebugLog(anglerState.ToString());

			OWAudioSource _loopSource = __instance.GetValue<OWAudioSource>("_loopSource");
			//this patch is exactly the same as the original code, plus a null check
			if (Locator.GetAudioManager() is AudioManager audioManager && audioManager != null)
			{
				switch (anglerState)
				{
					case AnglerfishController.AnglerState.Lurking:
						_loopSource.AssignAudioLibraryClip(global::AudioType.DBAnglerfishLurking_LP);
						_loopSource.FadeIn(0.5f, true, false, 1f);
						return;
					case AnglerfishController.AnglerState.Chasing:
						_loopSource.AssignAudioLibraryClip(global::AudioType.DBAnglerfishChasing_LP);
						_loopSource.FadeIn(0.5f, true, false, 1f);
						return;
				}
				_loopSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.STOP, 0f);
			}
		}
	}
}