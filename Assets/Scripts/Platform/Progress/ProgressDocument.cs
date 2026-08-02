using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChaosArena.Platform
{
    /// <summary>
    /// Cloud-ready, device-mergeable player progress. Arrays are intentional:
    /// JsonUtility does not support Dictionary and their order is canonical.
    /// </summary>
    [Serializable]
    public sealed class ProgressDocument
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public int bestFloor;
        public ProgressDeviceCounters[] devices = new ProgressDeviceCounters[0];

        public static ProgressDocument Empty() => new();
    }

    [Serializable]
    public sealed class ProgressDeviceCounters
    {
        public string deviceId = string.Empty;
        public int totalRuns;
        public int totalCoins;
        public int totalKills;
    }

    public static class ProgressDocumentCodec
    {
        public static bool TryDeserialize(string json, out ProgressDocument document)
        {
            document = ProgressDocument.Empty();
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                var parsed = JsonUtility.FromJson<ProgressDocument>(json);
                if (!ProgressDocumentValidation.TryNormalize(parsed, out document))
                    return false;

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static string Serialize(ProgressDocument document)
        {
            if (!ProgressDocumentValidation.TryNormalize(document, out var normalized))
                throw new ArgumentException("Progress document is invalid.", nameof(document));

            return JsonUtility.ToJson(normalized);
        }
    }

    public static class ProgressDocumentMerger
    {
        /// <summary>
        /// Merges grow-only counters by device and returns a canonical document.
        /// This operation is commutative, associative, and idempotent.
        /// </summary>
        public static bool TryMerge(ProgressDocument left, ProgressDocument right, out ProgressDocument merged)
        {
            merged = ProgressDocument.Empty();
            if (!ProgressDocumentValidation.TryNormalize(left, out var normalizedLeft) ||
                !ProgressDocumentValidation.TryNormalize(right, out var normalizedRight))
                return false;

            var countersByDevice = new Dictionary<string, ProgressDeviceCounters>(StringComparer.Ordinal);
            AddOrMerge(countersByDevice, normalizedLeft.devices);
            AddOrMerge(countersByDevice, normalizedRight.devices);

            var devices = new List<ProgressDeviceCounters>(countersByDevice.Values);
            devices.Sort(ProgressDocumentValidation.CompareDeviceIds);
            merged = new ProgressDocument
            {
                schemaVersion = ProgressDocument.CurrentSchemaVersion,
                bestFloor = Math.Max(normalizedLeft.bestFloor, normalizedRight.bestFloor),
                devices = devices.ToArray()
            };
            return true;
        }

        private static void AddOrMerge(
            IDictionary<string, ProgressDeviceCounters> countersByDevice,
            IEnumerable<ProgressDeviceCounters> counters)
        {
            foreach (var counter in counters)
            {
                if (!countersByDevice.TryGetValue(counter.deviceId, out var existing))
                {
                    countersByDevice.Add(counter.deviceId, Clone(counter));
                    continue;
                }

                existing.totalRuns = Math.Max(existing.totalRuns, counter.totalRuns);
                existing.totalCoins = Math.Max(existing.totalCoins, counter.totalCoins);
                existing.totalKills = Math.Max(existing.totalKills, counter.totalKills);
            }
        }

        internal static ProgressDeviceCounters Clone(ProgressDeviceCounters source) => new()
        {
            deviceId = source.deviceId,
            totalRuns = source.totalRuns,
            totalCoins = source.totalCoins,
            totalKills = source.totalKills
        };
    }

    internal static class ProgressDocumentValidation
    {
        public static bool TryNormalize(ProgressDocument source, out ProgressDocument normalized)
        {
            normalized = ProgressDocument.Empty();
            if (source == null || source.schemaVersion != ProgressDocument.CurrentSchemaVersion || source.bestFloor < 0)
                return false;

            var inputDevices = source.devices ?? new ProgressDeviceCounters[0];
            var deviceIds = new HashSet<string>(StringComparer.Ordinal);
            var devices = new List<ProgressDeviceCounters>(inputDevices.Length);
            foreach (var counter in inputDevices)
            {
                if (counter == null || !IsValidDeviceId(counter.deviceId) ||
                    counter.totalRuns < 0 || counter.totalCoins < 0 || counter.totalKills < 0 ||
                    !deviceIds.Add(counter.deviceId))
                    return false;

                devices.Add(ProgressDocumentMerger.Clone(counter));
            }

            devices.Sort(CompareDeviceIds);
            normalized = new ProgressDocument
            {
                schemaVersion = ProgressDocument.CurrentSchemaVersion,
                bestFloor = source.bestFloor,
                devices = devices.ToArray()
            };
            return true;
        }

        public static bool IsValidDeviceId(string deviceId) => !string.IsNullOrWhiteSpace(deviceId);

        public static int CompareDeviceIds(ProgressDeviceCounters left, ProgressDeviceCounters right) =>
            string.CompareOrdinal(left.deviceId, right.deviceId);
    }
}
