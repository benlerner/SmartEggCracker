using Harmony;
using Klei.AI;
using System;
using System.Collections.Generic;

namespace SmartEggCracker
{
    public class Patches
    {
        public const bool __DEBUG = true;

        // The name of the egg cracker's storage object in code. May be brittle.
        public const String __EGG_CRACKER_NAME = "EggCrackerComplete (Storage)";

        public static class Mod_OnLoad
        {
            public static void OnLoad()
            {
                Debug.Log("Smart Egg Cracker loaded.");
            }
        }

        [HarmonyPatch(typeof(FetchManager))]
        [HarmonyPatch("FindFetchTarget")]
        [HarmonyPatch(
            new Type[] {
                typeof(Storage),
                typeof(TagBits ),
                typeof(TagBits ),
                typeof(TagBits ),
                typeof(float)},
            new ArgumentType[]{
                ArgumentType.Normal,
                ArgumentType.Ref,
                ArgumentType.Ref,
                ArgumentType.Ref,
                ArgumentType.Normal }
            )]

        public class EggCracker_Fetch_Patch
        {

            static AccessTools.FieldRef<FetchManager, List<FetchManager.Pickup>> pickupListRef =
                AccessTools.FieldRefAccess<FetchManager, List<FetchManager.Pickup>>("pickups");

            public static void Prefix(
                Storage destination,
                ref TagBits tag_bits,
                ref TagBits required_tags,
                ref TagBits forbid_tags,
                float required_amount)
            {
                if (!__DEBUG)
                {
                    return;
                }

                // only display log messages for egg cracker errands
                if (!destination.ToString().Contains(__EGG_CRACKER_NAME))
                {
                    return;
                }

                string formatted_arguments = "\n\nFetch chore for egg cracker:";

                formatted_arguments += "\nTag bits:\n" + string.Join<Tag>(",", (IEnumerable<Tag>)tag_bits.GetTagsVerySlow());

                formatted_arguments += "\nRequired tags:\n" + string.Join<Tag>(",", (IEnumerable<Tag>)required_tags.GetTagsVerySlow());

                formatted_arguments += "\nForbid tags:\n" + string.Join<Tag>(",", (IEnumerable<Tag>)forbid_tags.GetTagsVerySlow());

                formatted_arguments += "\nRequired amount:\n" + required_amount.ToString();

                formatted_arguments += "\n\n";

                Debug.Log("Getting target for fetch chore:\n" + formatted_arguments);

            }

            public static void Postfix(FetchManager __instance,
    Storage destination,
    ref TagBits tag_bits,
    ref TagBits required_tags,
    ref TagBits forbid_tags,
    float required_amount,
    ref Pickupable __result)
            {

                // only do this for the egg cracker
                if (!destination.ToString().Contains(__EGG_CRACKER_NAME))
                {
                    return;
                }
                List<FetchManager.Pickup> pickup_list = pickupListRef(__instance);

                List<Pickupable> egg_list = new List<Pickupable>();
                if (__DEBUG)
                {
                    Debug.Log(String.Format("Number of pickupbables available: {0}", pickup_list.Count));
                }

                // get a list of all the available eggs for this fetch order
                foreach (FetchManager.Pickup pickup in pickup_list)
                {
                    if (__DEBUG)
                    {
                        string debug_output = "Checking item on list:\n";

                        debug_output += "Position:\n";
                        UnityEngine.Vector3 pos = pickup.pickupable.GetTargetPoint();
                        debug_output += String.Format("X: {0}  Y: {1}  Z: {2}\n", pos.x, pos.y, pos.z);
                        debug_output += "\nTag bits:\n" + string.Join<Tag>(",", (IEnumerable<Tag>)tag_bits.GetTagsVerySlow());

                        // TODO: use below code taken from FetchManager.IsFetchablePickup() to log if each pickup is valid
                        // pickup_id.HasAnyTags_AssumeLaundered(ref tag_bits) && pickup_id.HasAllTags_AssumeLaundered(ref required_tags) && !pickup_id.HasAnyTags_AssumeLaundered(ref forbid_tags) && (!((Object)source != (Object)null) || (source.ignoreSourcePriority || !destination.ShouldOnlyTransferFromLowerPriority || !(destination.masterPriority <= source.masterPriority)) && (destination.storageNetworkID == -1 || destination.storageNetworkID != source.storageNetworkID));


                        debug_output += "------\n\n";

                        Debug.Log(debug_output);
                     }

                    if (FetchManager.IsFetchablePickup(pickup.pickupable, ref tag_bits, ref required_tags, ref forbid_tags, destination))
                    {
                        egg_list.Add(pickup.pickupable);
                        
                    }
                }

                if (egg_list.Count == 0)
                {
                    __result = (Pickupable)null;
                }

                // sort the list of eggs by incubation in ascending order (lowest incubation first)
                int sort_egg_incubation(Pickupable x, Pickupable y)
                {
                    double x_incubation = Math.Round((x.gameObject.GetAmounts().Get(Db.Get().Amounts.Incubation)).value, 5);
                    double y_incubation = Math.Round((y.gameObject.GetAmounts().Get(Db.Get().Amounts.Incubation)).value, 5);

                    return x_incubation.CompareTo(y_incubation);
                }

                // return the first egg in the list (least incubation)
                egg_list.Sort(sort_egg_incubation);

                if (__DEBUG)
                {
                    string debug_output = "Sorted list of eggs:\n";

                    foreach (Pickupable p in egg_list)
                    {
                        debug_output += "Position:\n";
                        UnityEngine.Vector3 pos = p.GetTargetPoint();
                        debug_output += String.Format("X: {0}  Y: {1}  Z: {2}\n", pos.x, pos.y, pos.z);

                        debug_output += String.Format("Incubation: {0}\n", Math.Round((p.gameObject.GetAmounts().Get(Db.Get().Amounts.Incubation)).value, 5));

                        debug_output += "------\n\n";
                    }

                    Debug.Log(debug_output);
                }

                __result = egg_list[0];
            }
        }
    }
}
