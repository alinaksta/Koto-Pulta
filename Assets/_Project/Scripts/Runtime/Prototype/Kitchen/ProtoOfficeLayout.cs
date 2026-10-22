#if UNITY_EDITOR
using Game.Characters;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Editor: builds the office niches into the scene. The side desks are split into niches by glass partitions;
    /// every niche gets a computer and a chair copied from the office model and a one-seat table where a customer
    /// sits down after ordering. Everything is created as regular scene objects that can be moved by hand afterwards.
    /// </summary>
    internal static class ProtoOfficeLayout
    {
        public const string RootName = "OfficeNiches";

        private const string ComputerName = "computer";
        private const string ChairName = "OfficeChair";
        private const float AskDistance = 0.9f;
        private const string SignName = "NumberSign";
        private const string SignMaterialGuid = "6cfb8e98a8204c2986aa35c34dd59db4";
        private const string SignFontGuid = "236fde7f73574004aa2ce1c1579e2e44";
        // Above the niche computer, in niche space (z = toward the wall).
        private static readonly Vector3 s_signLocalPosition = new Vector3(0f, 2.15f, 1.1f);

        /// <summary>Office props replaced by gameplay prefabs at the same spot (cashier computer, bin).</summary>
        private static readonly string[] s_replacedParts = { "Computer", "Cylinder" };

        // Desk partitions in world XZ (office at scale 0.60625): end near the room center -> end at the wall.
        private static readonly Vector4[] s_leftDesk =
        {
            new Vector4(-11.34f, -3.91f, -12.99f, -4.68f),
            new Vector4(-11.80f, -1.38f, -13.54f, -1.66f),
            new Vector4(-11.80f,  1.38f, -13.54f,  1.66f),
            new Vector4(-11.34f,  3.91f, -12.99f,  4.68f),
        };
        private static readonly Vector4[] s_rightDesk =
        {
            new Vector4(10.98f, -4.89f, 12.54f, -5.95f),
            new Vector4(11.62f, -2.71f, 13.33f, -3.24f),
            new Vector4(11.86f,  0.00f, 13.60f,  0.00f),
            new Vector4(11.62f,  2.71f, 13.33f,  3.24f),
            new Vector4(10.98f,  4.89f, 12.54f,  5.95f),
        };

        // The niche that already has the office's own computer and chair (left desk, middle).
        private static readonly Niche s_source = new Niche(s_leftDesk[1], s_leftDesk[2]);

        public static void Bake(Transform parent, GameObject office)
        {
            Transform old = parent.Find(RootName);
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Bake office niches");
            root.transform.SetParent(parent, false);

            foreach (string part in s_replacedParts)
            {
                Transform replaced = FindDeep(office.transform, part);
                if (replaced != null)
                {
                    Undo.RecordObject(replaced.gameObject, "Hide office prop");
                    replaced.gameObject.SetActive(false);
                }
            }

            Transform computer = FindDeep(office.transform, ComputerName);
            Transform chair = FindDeep(office.transform, ChairName);

            var niches = new List<Niche>();
            AddNiches(s_leftDesk, niches);
            AddNiches(s_rightDesk, niches);

            int number = 1;
            foreach (Niche niche in niches)
            {
                var nicheRoot = new GameObject($"Niche_{number}");
                nicheRoot.transform.SetParent(root.transform, false);
                nicheRoot.transform.SetPositionAndRotation(niche.Inner3, Quaternion.LookRotation(niche.Outward3, Vector3.up));

                Transform nicheChair = chair;
                if (Vector2.Distance(niche.Inner, s_source.Inner) > 0.1f)
                {
                    if (computer != null)
                        CopyInto(computer, niche, nicheRoot.transform);
                    if (chair != null)
                        nicheChair = CopyInto(chair, niche, nicheRoot.transform);
                }

                Vector3 seat = nicheChair != null ? Flat(GetRendererBounds(nicheChair).center) : niche.Inner3 - niche.Outward3 * 0.3f;
                CreateTable(number, seat, niche, nicheRoot.transform);
                CreateSign(number, nicheRoot.transform);
                number++;
            }
        }

        /// <summary>
        /// Adds the number sign to niches that don't have one yet. Returns how many were added.
        /// </summary>
        public static int AddMissingSigns(Transform kitchen)
        {
            Transform root = kitchen.Find(RootName);
            if (root == null)
                return 0;

            int added = 0;
            foreach (Transform niche in root)
            {
                if (niche.Find(SignName) != null)
                    continue;

                Table table = niche.GetComponentInChildren<Table>(true);
                if (table == null)
                    continue;

                CreateSign(table.TableNumber, niche);
                added++;
            }

            return added;
        }

        /// <summary>
        /// A dark plate with the table number, hanging above the niche computer and readable from the room.
        /// </summary>
        private static void CreateSign(int number, Transform niche)
        {
            var sign = new GameObject(SignName);
            Undo.RegisterCreatedObjectUndo(sign, "Add niche sign");
            sign.transform.SetParent(niche, false);
            sign.transform.localPosition = s_signLocalPosition;
            sign.transform.localRotation = Quaternion.identity;

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plate.name = "Plate";
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            plate.transform.SetParent(sign.transform, false);
            plate.transform.localScale = new Vector3(0.5f, 0.38f, 1f);
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(SignMaterialGuid));
            if (material != null)
                plate.GetComponent<MeshRenderer>().sharedMaterial = material;

            var label = new GameObject("Number");
            label.transform.SetParent(sign.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            var text = label.AddComponent<TextMeshPro>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(SignFontGuid));
            if (font != null)
                text.font = font;
            text.text = number.ToString();
            text.fontSize = 3f;
            text.color = new Color(1f, 0.85f, 0.3f);
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(0.5f, 0.38f);
        }

        private static void AddNiches(Vector4[] partitions, List<Niche> niches)
        {
            for (int i = 0; i + 1 < partitions.Length; i++)
                niches.Add(new Niche(partitions[i], partitions[i + 1]));
        }

        /// <summary>
        /// Copies an office prop from the source niche into another niche, keeping its place relative to the niche.
        /// </summary>
        private static Transform CopyInto(Transform original, Niche to, Transform parent)
        {
            Quaternion turn = Quaternion.FromToRotation(s_source.Outward3, to.Outward3);
            Vector3 offset = original.position - s_source.Inner3;

            GameObject copy = Object.Instantiate(original.gameObject, parent);
            copy.name = original.name;
            copy.SetActive(true);
            copy.transform.SetPositionAndRotation(to.Inner3 + turn * offset, turn * original.rotation);
            copy.transform.localScale = Vector3.Scale(original.lossyScale, Inverse(parent.lossyScale));
            return copy.transform;
        }

        private static void CreateTable(int number, Vector3 seatPosition, Niche niche, Transform parent)
        {
            var root = new GameObject($"Table_{number}");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(seatPosition, Quaternion.LookRotation(niche.Outward3, Vector3.up));

            var seatObject = new GameObject("Seat");
            seatObject.transform.SetParent(root.transform, false);
            var spawn = new GameObject("CustomerSpawnOrigin").transform;
            spawn.SetParent(seatObject.transform, false);
            var ask = new GameObject("CustomerAskOrigin").transform;
            ask.SetParent(seatObject.transform, false);
            ask.position = seatPosition - niche.Outward3 * AskDistance;

            var seat = seatObject.AddComponent<Seat>();
            var seatData = new SerializedObject(seat);
            seatData.FindProperty("_customerSpawnOrigin").objectReferenceValue = spawn;
            seatData.FindProperty("_customerAskOrigin").objectReferenceValue = ask;
            seatData.FindProperty("_hasChairModel").boolValue = true;
            seatData.ApplyModifiedPropertiesWithoutUndo();

            var table = root.AddComponent<Table>();
            var tableData = new SerializedObject(table);
            tableData.FindProperty("_tableNumber").intValue = number;
            tableData.FindProperty("_registerAutomatically").boolValue = true;
            SerializedProperty seats = tableData.FindProperty("_seats");
            seats.arraySize = 1;
            seats.GetArrayElementAtIndex(0).objectReferenceValue = seat;
            tableData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds GetRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Vector3 Inverse(Vector3 v) => new Vector3(1f / v.x, 1f / v.y, 1f / v.z);

        private static Vector3 Flat(Vector3 point) => new Vector3(point.x, 0f, point.z);

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;

            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>A desk niche between two partitions (x, z of the inner end; x, z of the outer end).</summary>
        private readonly struct Niche
        {
            public readonly Vector2 Inner;
            public readonly Vector2 Outward;

            public Niche(Vector4 a, Vector4 b)
            {
                Vector2 aInner = new Vector2(a.x, a.y), aOuter = new Vector2(a.z, a.w);
                Vector2 bInner = new Vector2(b.x, b.y), bOuter = new Vector2(b.z, b.w);
                Inner = (aInner + bInner) * 0.5f;
                Outward = ((aOuter - aInner).normalized + (bOuter - bInner).normalized).normalized;
            }

            public Vector3 Inner3 => new Vector3(Inner.x, 0f, Inner.y);
            public Vector3 Outward3 => new Vector3(Outward.x, 0f, Outward.y);
        }
    }
}
#endif
