using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TcgEngine;
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector;
using System.Reflection;
using Sirenix.Utilities;
using System.Linq;


public class DuplicateFinder : OdinMenuEditorWindow
{
    [MenuItem("Tools/Moratelli/Duplicates Finder")]
    private static void OpenEditor() => GetWindow<DuplicateFinder>();

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.Config.DrawSearchToolbar = true;

        var assets = Resources.LoadAll("")
            .Where(obj => obj.GetType().GetField("id") != null)
            .OrderBy(obj => obj.ToString())
            .GroupBy(x => new { id = x.GetType().GetField("id").GetValue(x), type = x.GetType().Name })
            .Where(group => group.Sum(x => 1) > 1);

        if (assets.Count() == 0)
        {
            tree.Add("Everything good!", null, SdfIconType.HandThumbsUpFill);
        }
        else
        {
            tree.Add("There are duplications!", null, SdfIconType.HandThumbsDownFill);
        }

        foreach (var group in assets)
        {
            foreach (var item in group)
            {
                tree.Add($"{group.Key.id}/{item.name}", item);
            }
        }

        return tree;
    }
}

public class CardFinderForm
{
    [VerticalGroup("Form", PaddingBottom = 50)]
    public string cardName = "";
    [VerticalGroup("Form")]
    public AbilityData selectedAbility = null;
    [VerticalGroup("Form")]
    public AbilityTrigger selectedTrigger = AbilityTrigger.None;
    [VerticalGroup("Form")]
    public TeamData selectedTeam = null;
    [VerticalGroup("Form")]
    public RarityData selectedRarity = null;
    [VerticalGroup("Form")]
    [TextArea(3, 5)]
    public string text = "";
    [VerticalGroup("Form")]
    public TraitData selectedTrait = null;
    [VerticalGroup("Form")]
    public PackData selectedPack = null;

    private OdinMenuEditorWindow parent;

    public CardFinderForm(OdinMenuEditorWindow parent)
    {
        this.parent = parent;
    }

    [VerticalGroup("Actions")]
    [HorizontalGroup("Actions/Filter")]
    [Button(ButtonSizes.Large, Icon = SdfIconType.TrashFill)]
    private void ClearFilters()
    {
        selectedTrigger = AbilityTrigger.None;
        selectedAbility = null;
        cardName = "";
        selectedTeam = null;
        selectedRarity = null;
        text = "";
        selectedTrait = null;
        selectedPack = null;
        parent.ForceMenuTreeRebuild();
    }

    [HorizontalGroup("Actions/Filter")]
    [Button(ButtonSizes.Large, Icon = SdfIconType.FunnelFill)]
    private void Filter()
    {
        parent.ForceMenuTreeRebuild();
    }
}

public class CardFinder : OdinMenuEditorWindow
{
    [MenuItem("Tools/Moratelli/Card Finder")]
    private static void OpenEditor() => GetWindow<CardFinder>();

    private CardFinderForm form;
    private CardData[] cardDataAssets;

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        form ??= new CardFinderForm(this);
        tree.Config.DrawSearchToolbar = true;
        tree.Add("Filters", form);

        cardDataAssets = Resources.LoadAll<CardData>("").OrderBy(card => card.name).ToArray();

        foreach (CardData cardData in cardDataAssets)
        {
            bool show = true;
            if (form.selectedAbility != null && cardData.abilities.FirstOrDefault(ability => ability.id == form.selectedAbility.id) == null)
            {
                show = false;
            }

            if (form.selectedTrigger != AbilityTrigger.None && cardData.abilities.FirstOrDefault(ability => ability.trigger == form.selectedTrigger) == null)
            {
                show = false;
            }

            if (form.cardName != "" && cardData.name.ToLower().Contains(form.cardName.ToLower()) == false)
            {
                show = false;
            }

            if (form.text != "" && cardData.text.ToLower().Contains(form.text.ToLower()) == false)
            {
                show = false;
            }

            if (form.selectedPack != null && cardData.packs.FirstOrDefault(pack => pack.id == form.selectedPack.id) == null)
            {
                show = false;
            }

            if (form.selectedTeam != null && cardData.team.id != form.selectedTeam.id)
            {
                show = false;
            }

            if (form.selectedTrait != null && cardData.traits.FirstOrDefault(pack => pack.id == form.selectedTrait.id) == null)
            {
                show = false;
            }

            if (form.selectedRarity != null && cardData.rarity.id != form.selectedRarity.id)
            {
                show = false;
            }

            if (show)
            {
                tree.Add($"Card/{cardData.name}", cardData);
            }
        }

        return tree;
    }

    protected override void OnBeginDrawEditors()
    {
        if (MenuTree == null) return;

        SirenixEditorGUI.BeginHorizontalToolbar(MenuTree.Config.SearchToolbarHeight);
        {
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Clone")))
            {
                var selected = MenuTree.Selection.SelectedValue;

                if (selected == null || !selected.GetType().IsSubclassOf(typeof(ScriptableObject))) return;

                Type selectedType = selected.GetType();

                var objToSelect = EditorUtils.Clone(selectedType, selected);

                if (objToSelect != null)
                {
                    TrySelectMenuItemWithObject(objToSelect);
                    this.ForceMenuTreeRebuild();
                }
            }

            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Rename file")))
            {
                var selected = MenuTree.Selection.SelectedValue as UnityEngine.Object;

                var path = AssetDatabase.GetAssetPath(selected);

                var propertyInfo = selected.GetType().GetField("id");

                if (propertyInfo != null)
                {
                    var id = propertyInfo.GetValue(selected) as string;
                    AssetDatabase.RenameAsset(path, id);
                }
            }

            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Delete")))
            {
                var selected = MenuTree.Selection.SelectedValue as UnityEngine.Object;
                if (EditorUtility.DisplayDialog("Delete", $"Are you sure you want to delete \"{selected.name}\" ({selected.GetType()})?", "Yes", "No"))
                {
                    var path = AssetDatabase.GetAssetPath(selected);
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }
}


public class ScriptableObjectFinderForm
{
    private string[] types;
    [ValueDropdown("types")]
    [VerticalGroup("Form", PaddingBottom = 50)]
    public string selectedType = "";

    private OdinMenuEditorWindow parent;

    public ScriptableObjectFinderForm(OdinMenuEditorWindow parent)
    {
        types = typeof(CardData)
            .Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ScriptableObject)))
            .Select(type => type.ToString()).ToArray();
        this.parent = parent;
    }

    [VerticalGroup("Actions")]
    [HorizontalGroup("Actions/Filter")]
    [Button(ButtonSizes.Large, Icon = SdfIconType.TrashFill)]
    private void ClearFilters()
    {
        selectedType = "";
        parent.ForceMenuTreeRebuild();
    }

    [HorizontalGroup("Actions/Filter")]
    [Button(ButtonSizes.Large, Icon = SdfIconType.FunnelFill)]
    private void Filter()
    {
        parent.ForceMenuTreeRebuild();
    }
}

public class ScriptableObjectFinder : OdinMenuEditorWindow
{
    [MenuItem("Tools/Moratelli/SO Finder")]
    private static void OpenEditor() => GetWindow<ScriptableObjectFinder>();

    private ScriptableObjectFinderForm form;

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        form ??= new ScriptableObjectFinderForm(this);
        tree.Config.DrawSearchToolbar = true;
        tree.Add("Filters", form);
        var asm = typeof(ConditionData).Assembly;

        ScriptableObject[] assets = Resources.LoadAll<ScriptableObject>("").OrderBy(obj => obj.ToString()).ToArray();

        foreach (ScriptableObject asset in assets)
        {
            bool show = true;

            if (asset.GetType().Name.Contains("TcgEngine."))
            {
                show = false;
            }

            if (form.selectedType != "")
            {
                var search = tree.Config.SearchTerm;
                var type = asm.GetType(form.selectedType);

                if (type == null)
                {
                    show = false;
                }
                else
                {
                    show = asset.GetType() == type || asset.GetType().IsSubclassOf(type);
                }
            }

            if (show)
            {
                tree.Add($"{asset.GetType().Name.Split("TcgEngine.")[0]}/{asset.name}", asset);
            }
        }

        return tree;
    }

    protected override void OnBeginDrawEditors()
    {
        if (MenuTree == null) return;

        SirenixEditorGUI.BeginHorizontalToolbar(MenuTree.Config.SearchToolbarHeight);
        {
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Clone")))
            {
                var selected = MenuTree.Selection.SelectedValue;

                if (selected == null || !selected.GetType().IsSubclassOf(typeof(ScriptableObject))) return;

                Type selectedType = selected.GetType();

                var objToSelect = EditorUtils.Clone(selectedType, selected);

                if (objToSelect != null)
                {
                    TrySelectMenuItemWithObject(objToSelect);
                }
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }
}

public class CardEditor : OdinMenuEditorWindow
{
    [MenuItem("Tools/Moratelli/Card Manager")]
    private static void OpenEditor() => GetWindow<CardEditor>();

    public static bool ShowNotNull(UnityEngine.Object value)
    {
        return value != null;
    }

    public static void CloneAction<T>(object root, InspectorProperty property, T value)
    {
        if (value == null) return;

        string fieldName;
        if (property.Parent.Name.Contains("#") || property.Parent.Name.Contains("$"))
        {
            fieldName = property.Name;
        }
        else
        {
            fieldName = property.Parent.Name;
        }

        var field = root.GetType().GetField(fieldName);
        if (field != null && field.FieldType.IsArray)
        {
            var array = field.GetValue(root) as T[];
            T item = (T)EditorUtils.Clone(value.GetType(), value);

            if (item != null)
            {
                Array.Resize(ref array, array.Length + 1);
                array[^1] = item;
                field.SetValue(root, array);
            }
        }
        else if (field != null)
        {
            T item = (T)EditorUtils.Clone(value.GetType(), value);

            if (item != null)
            {
                field.SetValue(root, item);
            }
        }
    }

    static public void CreateNewAction(object root, InspectorProperty property)
    {
        string fieldName;

        if (property.Parent.Name.Contains("#") || property.Parent.Name.Contains("$"))
        {
            fieldName = property.Name;
        }
        else
        {
            fieldName = property.Parent.Name;
        }

        FieldInfo field = root.GetType().GetField(fieldName);

        if (field != null && field.FieldType.IsArray)
        {
            var array = field.GetValue(root) as object[];
            object item = EditorUtils.Clone(field.FieldType.GetElementType(), null, false);

            if (item != null)
            {
                Array.Resize(ref array, array.Length + 1);
                array[^1] = item;
                field.SetValue(root, array);
            }
        }
        else if (field != null)
        {
            object item = EditorUtils.Clone(field.FieldType, null, false);

            if (item != null)
            {
                field.SetValue(root, item);
            }
        }
    }

    public static void CreateNewActionButton(object root, InspectorProperty property)
    {
        if (SirenixEditorGUI.ToolbarButton(SdfIconType.PlusCircleDotted))
        {
            CreateNewAction(root, property);
        }
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.Config.DrawSearchToolbar = true;

        tree.AddAllAssetsAtPath("Cards", "Assets/TcgEngine/Resources/Cards", typeof(CardData), true, true).SortMenuItemsByName();
        tree.AddAllAssetsAtPath("Abilities", "Assets/TcgEngine/Resources/Abilities", typeof(AbilityData), true, true).SortMenuItemsByName();
        tree.AddAllAssetsAtPath("Effects", "Assets/TcgEngine/Resources/Effects", typeof(EffectData), true, true).SortMenuItemsByName();
        tree.AddAllAssetsAtPath("Conditions", "Assets/TcgEngine/Resources/Conditions", typeof(ConditionData), true, true).SortMenuItemsByName();
        tree.AddAllAssetsAtPath("Filters", "Assets/TcgEngine/Resources/Conditions", typeof(FilterData), true, true).SortMenuItemsByName();
        tree.AddAllAssetsAtPath("Status", "Assets/TcgEngine/Resources/Status", typeof(StatusData), true, true).SortMenuItemsByName();

        return tree;
    }

    protected override void OnBeginDrawEditors()
    {
        if (MenuTree == null) return;

        SirenixEditorGUI.BeginHorizontalToolbar(MenuTree.Config.SearchToolbarHeight);
        {
            GUILayout.Label("Card Editor");

            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Clone")))
            {
                var selected = MenuTree.Selection.SelectedValue as UnityEngine.Object;

                if (selected == null) return;

                Type selectedType = selected.GetType();

                var objToSelect = EditorUtils.Clone(selectedType, selected);

                if (objToSelect != null)
                {
                    TrySelectMenuItemWithObject(objToSelect);
                }
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }
}


public class CardDataProcessor : OdinAttributeProcessor<CardData>
{
    public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
    {
        base.ProcessChildMemberAttributes(parentProperty, member, attributes);

        if (member.Name == "abilities" || member.Name == "traits")
        {
            var attribute = attributes.GetAttribute<ListDrawerSettingsAttribute>();

            attribute.OnTitleBarGUI = "@CardEditor.CreateNewActionButton($root, $property)";
        }
    }
}

public class AbilityDataProcessor : OdinAttributeProcessor<AbilityData>
{
    public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
    {
        base.ProcessChildMemberAttributes(parentProperty, member, attributes);

        if (member.Name == "status" || member.Name == "chain_abilities")
        {
            var attribute = attributes.GetAttribute<ListDrawerSettingsAttribute>();

            attribute.OnTitleBarGUI = "@CardEditor.CreateNewActionButton($root, $property)";
        }
    }
}

public class EditorUtils
{
    private static void UpdateForType<T>(Type type, T source, T destination)
    {
        FieldInfo[] myObjectFields = type.GetFields(
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        foreach (FieldInfo fi in myObjectFields)
        {
            if (fi.FieldType.IsArray)
            {
                if (fi.FieldType.GetElementType().IsSubclassOf(typeof(ScriptableObject)) || fi.FieldType.GetElementType().IsEnum)
                {
                    fi.SetValue(destination, (fi.GetValue(source) as Array).Clone());
                }
                else
                {
                    var sourceArray = fi.GetValue(source) as Array;
                    if (sourceArray == null)
                        continue;

                    var clonedArray = Array.CreateInstance(fi.FieldType.GetElementType(), sourceArray.Length);
                    for (var i = 0; i < sourceArray.Length; i++)
                    {
                        var value = sourceArray.GetValue(i);
                        if (value != null && typeof(ICloneable).IsAssignableFrom(value.GetType()))
                        {
                            clonedArray.SetValue(((ICloneable)value).Clone(), i);
                        }
                        else
                        {
                            clonedArray.SetValue(value, i);
                        }
                    }

                    fi.SetValue(destination, clonedArray);
                }
            }
            else
            {
                fi.SetValue(destination, fi.GetValue(source));
            }
        }
    }

    public static string GetFolder(Type selectedType)
    {
        string SOData = selectedType.ToString();

        if (selectedType.IsSubclassOf(typeof(ConditionData)))
        {
            SOData = "TcgEngine.ConditionData";
        }
        else if (selectedType.IsSubclassOf(typeof(FilterData)))
        {
            SOData = "TcgEngine.FilterData";
        }
        else if (selectedType.IsSubclassOf(typeof(EffectData)))
        {
            SOData = "TcgEngine.EffectData";
        }

        var folders = new Dictionary<string, string>
        {
            { "TcgEngine.CardData", "Cards" },
            { "TcgEngine.AbilityData", "Abilities" },
            { "TcgEngine.EffectData", "Effects" },
            { "TcgEngine.ConditionData", "Conditions" },
            { "TcgEngine.FilterData", "Filters" },
            { "TcgEngine.StatusData", "Status" },
            { "TcgEngine.TraitData", "Traits" },
            { "TcgEngine.TeamData", "Teams" },
            { "TcgEngine.RarityData", "Rarities" },
        };

        return folders.ContainsKey(SOData) ? $"Assets/TcgEngine/Resources/{folders[SOData]}" : "Assets/TcgEngine/Resources/";
    }

    public static T Clone<T>(Type selectedType, object selected, bool copyValues = true) where T : ScriptableObject
    {
        string folder = "";
        if (selected == null)
        {
            folder = GetFolder(selectedType);
        }
        else
        {
            string[] parts = AssetDatabase.GetAssetPath(selected as UnityEngine.Object).Split("/");
            Array.Resize(ref parts, parts.Length - 1);
            folder = string.Join("/", parts);
        }

        // Create new ScriptableObject instance
        T clone = ScriptableObject.CreateInstance(selectedType) as T;

        if (clone == null)
            return null;

        // Copy values if requested
        if (copyValues && selected != null)
            UpdateForType(selectedType, selected as T, clone);

        // Generate unique name
        string baseName = selected != null ? (selected as UnityEngine.Object).name : "New" + selectedType.Name;
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.asset");
        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        // Set id field to match filename
        var propertyInfo = clone.GetType().GetField("id");
        if (propertyInfo != null)
        {
            propertyInfo.SetValue(clone, fileName.ToLower().Replace(" ", "_"));
        }

        // Create and save asset
        AssetDatabase.CreateAsset(clone, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the new asset
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = clone;

        return clone;
    }

    public static object Clone(Type selectedType, object selected, bool copyValues = true)
    {
        return Clone<ScriptableObject>(selectedType, selected, copyValues);
    }
}
