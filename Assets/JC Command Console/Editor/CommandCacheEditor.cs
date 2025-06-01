using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using JetCreative.CommandConsolePro;
using System.Linq;

namespace JetCreative.CommandConsolePro.Editor
{
    [CustomEditor(typeof(CommandCache))]
    public class CommandCacheEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset visualTree;
        [SerializeField] private StyleSheet styleSheet;
        private VisualElement root;
        private CommandCache commandCache;
        public override VisualElement CreateInspectorGUI()
        {
            commandCache = target as CommandCache;
            root = new VisualElement();

            // Load the UXML file
            //var visualTree = Resources.Load<VisualTreeAsset>("CommandCacheEditor");
            if (visualTree != null)
            {
                visualTree.CloneTree(root);
            }
            else
            {
                // Fallback: create UI programmatically if UXML is not found
                CreateUIElements(root);
            }

            // Load the stylesheet
            //var styleSheet = Resources.Load<StyleSheet>("CommandCacheEditor");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            // Bind the UI elements
            BindUIElements(root);

            return root;
        }

        private void CreateUIElements(VisualElement root)
        {
            // Banner
            var banner = new VisualElement();
            banner.name = "banner";
            banner.AddToClassList("banner");

            var bannerTitle = new Label("Command Cache");
            bannerTitle.name = "banner-title";
            bannerTitle.AddToClassList("banner-title");
            banner.Add(bannerTitle);

            var bannerSubtitle = new Label("Command Console by Jet Creative");
            bannerSubtitle.name = "banner-subtitle";
            bannerSubtitle.AddToClassList("banner-subtitle");
            banner.Add(bannerSubtitle);

            root.Add(banner);

            // GitHub README Link
            var readmeLink = new Button();
            readmeLink.name = "readme-link";
            readmeLink.text = "📖 View README on GitHub";
            readmeLink.AddToClassList("readme-link");
            readmeLink.style.marginTop = 1;
            readmeLink.style.marginBottom = 1;
            readmeLink.style.backgroundColor = new Color(0.06f, 0.06f, 0.12f, 0.75f);
            readmeLink.style.color = Color.white;
            readmeLink.style.borderTopWidth = 0;
            readmeLink.style.borderBottomWidth = 0;
            readmeLink.style.borderLeftWidth = 0;
            readmeLink.style.borderRightWidth = 0;
            readmeLink.RegisterCallback<ClickEvent>(_ => Application.OpenURL("https://github.com/JetCreative/JCCommandConsole/blob/main/README.md"));
            root.Add(readmeLink);

            // Add bold line after banner
            var bannerSeparator = new VisualElement();
            bannerSeparator.AddToClassList("bold-separator");
            bannerSeparator.style.height = 2;
            bannerSeparator.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            bannerSeparator.style.marginTop = 2;
            bannerSeparator.style.marginBottom = 2;
            root.Add(bannerSeparator);

            // Settings Section
            var settingsSection = CreateSection("Settings", "settings-section");
            var settingsContent = new VisualElement();
            settingsContent.name = "settings-content";
            settingsContent.AddToClassList("section-content");

            // Add settings fields
            settingsContent.Add(CreateBoolSettingField("IncludePrivateMembers", "Include Private Members"));
            settingsContent.Add(CreateBoolSettingField("IncludeExampleCommands", "Include Example Commands"));
            
            settingsContent.Add(CreateThinSeparator());
            settingsContent.Add(CreateBoolSettingField("ShowIncludeNamespaces", "Show Include Namespaces"));

            // Custom Include Namespaces field with remove buttons
            var includeNamespacesContainer = new VisualElement();
            includeNamespacesContainer.name = "include-namespaces-container";
            includeNamespacesContainer.style.borderTopWidth = 0;
            includeNamespacesContainer.style.borderBottomWidth = 0;
            includeNamespacesContainer.style.borderLeftWidth = 0;
            includeNamespacesContainer.style.borderRightWidth = 0;
            settingsContent.Add(includeNamespacesContainer);

            settingsContent.Add(CreateThinSeparator());
            settingsContent.Add(CreateBoolSettingField("ShowExcludeNamespaces", "Show Exclude Namespaces"));

            // Custom Exclude Namespaces field with remove buttons
            var excludeNamespacesContainer = new VisualElement();
            excludeNamespacesContainer.name = "exclude-namespaces-container";
            excludeNamespacesContainer.style.borderTopWidth = 0;
            excludeNamespacesContainer.style.borderBottomWidth = 0;
            excludeNamespacesContainer.style.borderLeftWidth = 0;
            excludeNamespacesContainer.style.borderRightWidth = 0;
            settingsContent.Add(excludeNamespacesContainer);

            settingsSection.Add(settingsContent);
            root.Add(settingsSection);

            // Add bold line between main sections
            var mainSeparator = new VisualElement();
            mainSeparator.AddToClassList("bold-separator");
            mainSeparator.style.height = 2;
            mainSeparator.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            mainSeparator.style.marginTop = 2;
            mainSeparator.style.marginBottom = 2;
            root.Add(mainSeparator);

            // Command Cache Info Section
            var cacheInfoSection = CreateSection("Current Command Cache Info", "cache-info-section");
            var cacheInfoContent = new VisualElement();
            cacheInfoContent.name = "cache-info-content";
            cacheInfoContent.AddToClassList("section-content");

            // Command counts container
            var countsContainer = new VisualElement();
            countsContainer.name = "counts-container";
            countsContainer.AddToClassList("counts-container");

            var countsTitle = new Label("Command Counts:");
            countsTitle.AddToClassList("counts-title");
            countsContainer.Add(countsTitle);

            countsContainer.Add(CreateCountLabel("method-count"));
            countsContainer.Add(CreateCountLabel("property-get-count"));
            countsContainer.Add(CreateCountLabel("property-set-count"));
            countsContainer.Add(CreateCountLabel("field-count"));
            countsContainer.Add(CreateCountLabel("delegate-count"));
            countsContainer.Add(CreateCountLabel("total-count", "total-count-label"));

            cacheInfoContent.Add(countsContainer);

            // Add thin line before command lists
            var thinSeparator = new VisualElement();
            thinSeparator.AddToClassList("thin-separator");
            thinSeparator.style.height = 1;
            thinSeparator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            thinSeparator.style.marginTop = 5;
            thinSeparator.style.marginBottom = 5;
            cacheInfoContent.Add(thinSeparator);

            // Command names subsections
            cacheInfoContent.Add(CreateCommandNamesSubsection("Method Commands", "method-commands"));
            cacheInfoContent.Add(CreateThinSeparator());
            cacheInfoContent.Add(CreateCommandNamesSubsection("Property Get Commands", "property-get-commands"));
            cacheInfoContent.Add(CreateThinSeparator());
            cacheInfoContent.Add(CreateCommandNamesSubsection("Property Set Commands", "property-set-commands"));
            cacheInfoContent.Add(CreateThinSeparator());
            cacheInfoContent.Add(CreateCommandNamesSubsection("Field Commands", "field-commands"));
            cacheInfoContent.Add(CreateThinSeparator());
            cacheInfoContent.Add(CreateCommandNamesSubsection("Delegate Commands", "delegate-commands"));

            cacheInfoSection.Add(cacheInfoContent);
            root.Add(cacheInfoSection);
            
            // Add bold line before button
            var buttonSeparator = new VisualElement();
            buttonSeparator.AddToClassList("bold-separator");
            buttonSeparator.style.height = 2;
            buttonSeparator.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            buttonSeparator.style.marginTop = 2;
            buttonSeparator.style.marginBottom = 2;
            root.Add(buttonSeparator);

            // Regenerate Cache Button
            var regenerateButton = new Button();
            regenerateButton.name = "regenerate-button";
            regenerateButton.text = "Regenerate Cache";
            regenerateButton.AddToClassList("regenerate-button");
            root.Add(regenerateButton);
        }

        private VisualElement CreateBoolSettingField(string bindingPath, string labelText)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.alignItems = Align.Center;
            container.style.borderTopWidth = 0;
            container.style.borderBottomWidth = 0;
            container.style.borderLeftWidth = 0;
            container.style.borderRightWidth = 0;

            var label = new Label(labelText);
            label.style.flexGrow = 1;

            var toggle = new Toggle();
            toggle.bindingPath = bindingPath;
            toggle.style.flexShrink = 0;

            container.Add(label);
            container.Add(toggle);

            return container;
        }

        private Label CreateCountLabel(string name, string className = "count-label")
        {
            var label = new Label();
            label.name = name;
            label.AddToClassList(className);
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            return label;
        }

        private VisualElement CreateThinSeparator()
        {
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            separator.style.marginTop = 2;
            separator.style.marginBottom = 2;
            return separator;
        }

        private VisualElement CreateSection(string title, string sectionName)
        {
            var section = new Foldout();
            section.name = sectionName;
            section.text = title;
            section.value = true;
            section.AddToClassList("section");
            section.style.borderTopWidth = 0;
            section.style.borderBottomWidth = 0;
            section.style.borderLeftWidth = 0;
            section.style.borderRightWidth = 0;
            return section;
        }

        private VisualElement CreateCommandNamesSubsection(string title, string subsectionName)
        {
            var subsection = new Foldout();
            subsection.name = subsectionName;
            subsection.text = title;
            subsection.value = false;
            subsection.AddToClassList("subsection");
            subsection.style.borderTopWidth = 0;
            subsection.style.borderBottomWidth = 0;
            subsection.style.borderLeftWidth = 0;
            subsection.style.borderRightWidth = 0;

            var content = new VisualElement();
            content.name = subsectionName + "-content";
            content.AddToClassList("subsection-content");

            var scrollView = new ScrollView();
            scrollView.name = subsectionName + "-scroll";
            scrollView.AddToClassList("commands-scroll");
            scrollView.mode = ScrollViewMode.Vertical;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.style.maxHeight = 200;
            content.Add(scrollView);

            subsection.Add(content);
            return subsection;
        }

        private void CreateNamespaceList(VisualElement container, string propertyName, string listName)
        {
            //var commandCache = target as CommandCache;
            var property = serializedObject.FindProperty(propertyName);
            
            container.Clear();
            
            if (property != null && property.isArray)
            {
                for (int i = 0; i < property.arraySize; i++)
                {
                    var elementContainer = new VisualElement();
                    elementContainer.style.flexDirection = FlexDirection.Row;
                    elementContainer.style.alignItems = Align.Center;
                    elementContainer.style.justifyContent = Justify.SpaceBetween;
                    elementContainer.style.marginBottom = 2;

                    var elementProperty = property.GetArrayElementAtIndex(i);
                    var textField = new TextField();
                    textField.bindingPath = elementProperty.propertyPath;
                    textField.style.flexGrow = 1;
                    textField.style.borderTopWidth = 0;
                    textField.style.borderBottomWidth = 0;
                    textField.style.borderLeftWidth = 0;
                    textField.style.borderRightWidth = 0;

                    var removeButton = new Button();
                    removeButton.text = "×";
                    removeButton.style.width = 20;
                    removeButton.style.height = 20;
                    removeButton.style.flexShrink = 0;
                    removeButton.style.fontSize = 14;
                    removeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                    removeButton.style.backgroundColor = new Color(0.8f, 0.3f, 0.3f, 0.8f);
                    
                    int index = i; // Capture index for closure
                    removeButton.RegisterCallback<ClickEvent>(_ => RemoveNamespaceItem(propertyName, index));

                    elementContainer.Add(textField);
                    elementContainer.Add(removeButton);
                    container.Add(elementContainer);
                }
            }

            // Add button to add new items
            var addButton = new Button();
            addButton.text = $"+ Add {listName}";
            addButton.style.marginTop = 5;
            addButton.RegisterCallback<ClickEvent>(_ => AddNamespaceItem(propertyName));
            container.Add(addButton);
        }

        private void RemoveNamespaceItem(string propertyName, int index)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null && property.isArray && index >= 0 && index < property.arraySize)
            {
                property.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                
                // Refresh the namespace lists
                //var root = serializedObject.targetObject as CommandCache;
                UpdateNamespaceLists(root);
            }
        }

        private void AddNamespaceItem(string propertyName)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null && property.isArray)
            {
                property.arraySize++;
                var newElement = property.GetArrayElementAtIndex(property.arraySize - 1);
                newElement.stringValue = "";
                serializedObject.ApplyModifiedProperties();
                
                // Refresh the namespace lists
                //var root = serializedObject.targetObject as CommandCache;
                UpdateNamespaceLists(root);
            }
        }

        private void UpdateNamespaceLists(VisualElement root)
        {
            var includeContainer = root?.Q<VisualElement>("include-namespaces-container");
            var excludeContainer = root?.Q<VisualElement>("exclude-namespaces-container");
            
            if (includeContainer != null)
                CreateNamespaceList(includeContainer, "IncludeNamespaces", "Include Namespace");
            
            if (excludeContainer != null)
                CreateNamespaceList(excludeContainer, "ExcludeNamespaces", "Exclude Namespace");
        }

        private void BindUIElements(VisualElement root)
        {
            //var commandCache = target as CommandCache;

            // Bind property fields
            root.Bind(serializedObject);

            // Create namespace lists with remove buttons
            UpdateNamespaceLists(root);

            // Update command counts and visibility
            UpdateCommandCounts(root, commandCache);
            UpdateNamespaceFieldsVisibility(root, commandCache);
            UpdateCommandNamesList(root, commandCache);

            // Set up event handlers
            root.Q<Toggle>()?.TrackPropertyValue(serializedObject.FindProperty("ShowIncludeNamespaces"), 
                _ => UpdateNamespaceFieldsVisibility(root, commandCache));
            
            var showExcludeToggle = root.Q<Toggle>().parent.parent.Q<Toggle>();
            showExcludeToggle?.TrackPropertyValue(serializedObject.FindProperty("ShowExcludeNamespaces"), 
                _ => UpdateNamespaceFieldsVisibility(root, commandCache));

            // Regenerate button click handler
            var regenerateButton = root.Q<Button>("regenerate-button");
            regenerateButton?.RegisterCallback<ClickEvent>(_ => OnRegenerateCacheClicked());

            // Update command lists when foldouts are opened
            SetupCommandListUpdates(root, commandCache);
        }

        private void UpdateCommandCounts(VisualElement root, CommandCache commandCache)
        {
            root.Q<Label>("method-count").text = $"Method Commands: {commandCache.MethodCommands.Count}";
            root.Q<Label>("property-get-count").text = $"Property Get Commands: {commandCache.PropertyGetCommands.Count}";
            root.Q<Label>("property-set-count").text = $"Property Set Commands: {commandCache.PropertySetCommands.Count}";
            root.Q<Label>("field-count").text = $"Field Commands: {commandCache.FieldCommands.Count}";
            root.Q<Label>("delegate-count").text = $"Delegate Commands: {commandCache.DelegateCommands.Count}";

            int totalCommands = commandCache.MethodCommands.Count + 
                              commandCache.PropertyGetCommands.Count + 
                              commandCache.PropertySetCommands.Count + 
                              commandCache.FieldCommands.Count + 
                              commandCache.DelegateCommands.Count;

            root.Q<Label>("total-count").text = $"Total Commands: {totalCommands}";
        }

        private void UpdateNamespaceFieldsVisibility(VisualElement root, CommandCache commandCache)
        {
            var includeContainer = root.Q<VisualElement>("include-namespaces-container");
            var excludeContainer = root.Q<VisualElement>("exclude-namespaces-container");

            if (includeContainer != null)
                includeContainer.style.display = commandCache.ShowIncludeNamespaces ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (excludeContainer != null)
                excludeContainer.style.display = commandCache.ShowExcludeNamespaces ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateCommandNamesList(VisualElement root, CommandCache commandCache)
        {
            UpdateCommandList(root, "method-commands", commandCache.MethodCommands.Keys.ToList());
            UpdateCommandList(root, "property-get-commands", commandCache.PropertyGetCommands.Keys.ToList());
            UpdateCommandList(root, "property-set-commands", commandCache.PropertySetCommands.Keys.ToList());
            UpdateCommandList(root, "field-commands", commandCache.FieldCommands.Keys.ToList());
            UpdateCommandList(root, "delegate-commands", commandCache.DelegateCommands.Keys.ToList());
        }

        private void UpdateCommandList(VisualElement root, string subsectionName, System.Collections.Generic.List<string> commands)
        {
            var subsection = root.Q<Foldout>(subsectionName);
            if (subsection != null)
            {
                subsection.text = $"{subsection.text.Split('(')[0].Trim()} ({commands.Count})";
                
                var scrollView = root.Q<ScrollView>(subsectionName + "-scroll");
                scrollView?.Clear();

                if (commands.Count == 0)
                {
                    var noCommandsLabel = new Label("No commands found");
                    noCommandsLabel.AddToClassList("no-commands-label");
                    scrollView?.Add(noCommandsLabel);
                }
                else
                {
                    foreach (var command in commands.OrderBy(x => x))
                    {
                        var commandLabel = new Label(command);
                        commandLabel.style.flexGrow = 1;
                        commandLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                        commandLabel.AddToClassList("command-name-label");
                        scrollView?.Add(commandLabel);
                    }
                }
            }
        }

        private void SetupCommandListUpdates(VisualElement root, CommandCache commandCache)
        {
            var subsectionNames = new[] { "method-commands", "property-get-commands", "property-set-commands", "field-commands", "delegate-commands" };
            
            foreach (var name in subsectionNames)
            {
                var subsection = root.Q<Foldout>(name);
                subsection?.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue) // Only update when opening
                    {
                        UpdateCommandNamesList(root, commandCache);
                    }
                });
            }
        }

        private void OnRegenerateCacheClicked()
        {
            JCCommandConsolePro.Instance.GenerateCommandCache(commandCache.IncludePrivateMembers, commandCache.IncludeExampleCommands, commandCache.IncludeNamespaces.ToArray(), commandCache.ExcludeNamespaces.ToArray());
            
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }
}