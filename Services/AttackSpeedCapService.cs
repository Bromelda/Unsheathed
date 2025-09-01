using ProjectM;
using ProjectM.Shared;
using System.Collections.Generic;
using Unity.Entities;

namespace Unsheathed.Services
{
    public static class AttackSpeedCapService
    {
        private struct SavedCaps { public bool Had; public VampireAttributeCaps Val; }

        static readonly HashSet<Entity> Active = new();
        static readonly Dictionary<Entity, SavedCaps> Saved = new();

        public static bool TryLiftCaps(EntityManager em, Entity character)
        {
            if (!em.Exists(character) || Active.Contains(character)) return false;

            var rec = new SavedCaps { Had = false };
            if (em.HasComponent<VampireAttributeCaps>(character))
            {
                rec.Had = true;
                rec.Val = em.GetComponentData<VampireAttributeCaps>(character);
                em.RemoveComponent<VampireAttributeCaps>(character);
            }
            Saved[character] = rec;
            Active.Add(character);
            return true;
        }

        public static void RestoreCaps(EntityManager em, Entity character)
        {
            if (!Active.Remove(character)) return;
            if (!em.Exists(character)) { Saved.Remove(character); return; }

            if (Saved.Remove(character, out var rec) && rec.Had)
            {
                if (em.HasComponent<VampireAttributeCaps>(character))
                    em.SetComponentData(character, rec.Val);
                else
                    em.AddComponentData(character, rec.Val);
            }
        }

        public static List<Entity> GetActiveSnapshot()
        {
            var list = new List<Entity>(Active.Count);
            foreach (var e in Active) list.Add(e);
            return list;
        }
    }
}
