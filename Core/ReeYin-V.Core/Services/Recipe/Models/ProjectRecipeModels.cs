using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace ReeYin_V.Core.Services.Recipe
{
    [Serializable]
    public class ProjectRecipeNodeGroup : BindableBase
    {
        public ProjectRecipeNodeGroup()
        {
            AttachParameters(_parameters);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _parameters ??= new ObservableCollection<RecipeParamInfo>();
            AttachParameters(_parameters);
            RaisePropertyChanged(nameof(ParameterCount));
        }

        private int _serial = -1;
        public int Serial
        {
            get => _serial;
            set
            {
                if (SetProperty(ref _serial, value))
                {
                    RaisePropertyChanged(nameof(DisplayName));
                }
            }
        }

        private string _subjection = string.Empty;
        public string Subjection
        {
            get => _subjection;
            set
            {
                if (SetProperty(ref _subjection, value ?? string.Empty))
                {
                    RaisePropertyChanged(nameof(DisplayName));
                }
            }
        }

        private ObservableCollection<RecipeParamInfo> _parameters = new();
        public ObservableCollection<RecipeParamInfo> Parameters
        {
            get => _parameters;
            set
            {
                if (ReferenceEquals(_parameters, value))
                {
                    return;
                }

                DetachParameters(_parameters);
                SetProperty(ref _parameters, value ?? new ObservableCollection<RecipeParamInfo>());
                AttachParameters(_parameters);
                RaisePropertyChanged(nameof(ParameterCount));
            }
        }

        [JsonIgnore]
        public int ParameterCount => Parameters?.Count ?? 0;

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(Subjection)
            ? (Serial >= 0 ? $"Node {Serial}" : "Unnamed Node")
            : Subjection;

        public RecipeParamInfo FindParameter(string recipeKey)
        {
            return Parameters?
                .FirstOrDefault(item => string.Equals(item.RecipeKey, recipeKey, StringComparison.OrdinalIgnoreCase));
        }

        public ProjectRecipeNodeGroup CreateCopy()
        {
            ProjectRecipeNodeGroup copy = new()
            {
                Serial = Serial,
                Subjection = Subjection
            };

            foreach (RecipeParamInfo parameter in Parameters?.Where(item => item != null) ?? Enumerable.Empty<RecipeParamInfo>())
            {
                copy.Parameters.Add(parameter.CreateCopy());
            }

            return copy;
        }

        private void AttachParameters(ObservableCollection<RecipeParamInfo> parameters)
        {
            if (parameters == null)
            {
                return;
            }

            parameters.CollectionChanged -= Parameters_CollectionChanged;
            parameters.CollectionChanged += Parameters_CollectionChanged;
        }

        private void DetachParameters(ObservableCollection<RecipeParamInfo> parameters)
        {
            if (parameters == null)
            {
                return;
            }

            parameters.CollectionChanged -= Parameters_CollectionChanged;
        }

        private void Parameters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(ParameterCount));
        }
    }

    [Serializable]
    public class ProjectRecipeInfo : BindableBase
    {
        public ProjectRecipeInfo()
        {
            AttachGroups(_groups);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _groups ??= new ObservableCollection<ProjectRecipeNodeGroup>();
            AttachGroups(_groups);
            RaisePropertyChanged(nameof(NodeCount));
            RaisePropertyChanged(nameof(ParameterCount));
        }

        private Guid _id = Guid.NewGuid();
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value ?? string.Empty);
        }

        private string _category = string.Empty;
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value ?? string.Empty);
        }

        private string _version = "1.0.0";
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value ?? string.Empty);
        }

        private DateTime _createdAt = DateTime.Now;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        private DateTime _modifiedAt = DateTime.Now;
        public DateTime ModifiedAt
        {
            get => _modifiedAt;
            set => SetProperty(ref _modifiedAt, value);
        }

        private string _author = string.Empty;
        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value ?? string.Empty);
        }

        private string _lastModifiedBy = string.Empty;
        public string LastModifiedBy
        {
            get => _lastModifiedBy;
            set => SetProperty(ref _lastModifiedBy, value ?? string.Empty);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private bool _isCurrent;
        [JsonIgnore]
        public bool IsCurrent
        {
            get => _isCurrent;
            set => SetProperty(ref _isCurrent, value);
        }

        private ObservableCollection<ProjectRecipeNodeGroup> _groups = new();
        public ObservableCollection<ProjectRecipeNodeGroup> Groups
        {
            get => _groups;
            set
            {
                if (ReferenceEquals(_groups, value))
                {
                    return;
                }

                DetachGroups(_groups);
                SetProperty(ref _groups, value ?? new ObservableCollection<ProjectRecipeNodeGroup>());
                AttachGroups(_groups);
                RaisePropertyChanged(nameof(NodeCount));
                RaisePropertyChanged(nameof(ParameterCount));
            }
        }

        [JsonIgnore]
        public int NodeCount => Groups?.Count ?? 0;

        [JsonIgnore]
        public int ParameterCount => Groups?.Sum(item => item.ParameterCount) ?? 0;

        public ProjectRecipeNodeGroup FindGroup(int serial, string subjection)
        {
            string normalizedSubjection = subjection ?? string.Empty;

            return Groups?.FirstOrDefault(item =>
                item.Serial == serial &&
                string.Equals(item.Subjection ?? string.Empty, normalizedSubjection, StringComparison.OrdinalIgnoreCase));
        }

        public ProjectRecipeNodeGroup GetOrCreateGroup(int serial, string subjection)
        {
            ProjectRecipeNodeGroup group = FindGroup(serial, subjection);
            if (group != null)
            {
                return group;
            }

            group = Groups?.FirstOrDefault(item => item.Serial == serial);
            if (group != null)
            {
                group.Subjection = subjection ?? string.Empty;
                return group;
            }

            group = new ProjectRecipeNodeGroup
            {
                Serial = serial,
                Subjection = subjection ?? string.Empty
            };

            Groups ??= new ObservableCollection<ProjectRecipeNodeGroup>();
            Groups.Add(group);
            return group;
        }

        public RecipeParamInfo FindParameter(string recipeKey)
        {
            return Groups?
                .SelectMany(item => item.Parameters)
                .FirstOrDefault(item => string.Equals(item.RecipeKey, recipeKey, StringComparison.OrdinalIgnoreCase));
        }

        public ProjectRecipeInfo CreateCopy(string operatorName, string newName)
        {
            DateTime now = DateTime.Now;
            ProjectRecipeInfo copy = new()
            {
                Id = Guid.NewGuid(),
                Name = newName ?? string.Empty,
                Description = Description,
                Category = Category,
                Version = Version,
                CreatedAt = now,
                ModifiedAt = now,
                Author = string.IsNullOrWhiteSpace(Author) ? operatorName ?? string.Empty : Author,
                LastModifiedBy = operatorName ?? string.Empty,
                IsEnabled = IsEnabled
            };

            foreach (ProjectRecipeNodeGroup group in Groups?.Where(item => item != null) ?? Enumerable.Empty<ProjectRecipeNodeGroup>())
            {
                ProjectRecipeNodeGroup groupCopy = group.CreateCopy();
                foreach (RecipeParamInfo parameter in groupCopy.Parameters)
                {
                    parameter.Id = Guid.NewGuid();
                }

                copy.Groups.Add(groupCopy);
            }

            return copy;
        }

        public ProjectRecipeInfo CreateSnapshot()
        {
            ProjectRecipeInfo snapshot = new()
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Category = Category,
                Version = Version,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt,
                Author = Author,
                LastModifiedBy = LastModifiedBy,
                IsEnabled = IsEnabled,
                IsCurrent = IsCurrent
            };

            foreach (ProjectRecipeNodeGroup group in Groups?.Where(item => item != null) ?? Enumerable.Empty<ProjectRecipeNodeGroup>())
            {
                snapshot.Groups.Add(group.CreateCopy());
            }

            return snapshot;
        }

        private void AttachGroups(ObservableCollection<ProjectRecipeNodeGroup> groups)
        {
            if (groups == null)
            {
                return;
            }

            groups.CollectionChanged -= Groups_CollectionChanged;
            groups.CollectionChanged += Groups_CollectionChanged;

            foreach (ProjectRecipeNodeGroup group in groups)
            {
                group.PropertyChanged -= Group_PropertyChanged;
                group.PropertyChanged += Group_PropertyChanged;
            }
        }

        private void DetachGroups(ObservableCollection<ProjectRecipeNodeGroup> groups)
        {
            if (groups == null)
            {
                return;
            }

            groups.CollectionChanged -= Groups_CollectionChanged;

            foreach (ProjectRecipeNodeGroup group in groups)
            {
                group.PropertyChanged -= Group_PropertyChanged;
            }
        }

        private void Groups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ProjectRecipeNodeGroup group in e.NewItems.OfType<ProjectRecipeNodeGroup>())
                {
                    group.PropertyChanged -= Group_PropertyChanged;
                    group.PropertyChanged += Group_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (ProjectRecipeNodeGroup group in e.OldItems.OfType<ProjectRecipeNodeGroup>())
                {
                    group.PropertyChanged -= Group_PropertyChanged;
                }
            }

            RaisePropertyChanged(nameof(NodeCount));
            RaisePropertyChanged(nameof(ParameterCount));
        }

        private void Group_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProjectRecipeNodeGroup.ParameterCount))
            {
                RaisePropertyChanged(nameof(ParameterCount));
            }
        }
    }

    [Serializable]
    public class ProjectRecipeConfig : BindableBase
    {
        public ProjectRecipeConfig()
        {
            AttachRecipes(_recipes);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _recipes ??= new ObservableCollection<ProjectRecipeInfo>();
            AttachRecipes(_recipes);
            EnsureValidState();
        }

        private ObservableCollection<ProjectRecipeInfo> _recipes = new();
        public ObservableCollection<ProjectRecipeInfo> Recipes
        {
            get => _recipes;
            set
            {
                if (ReferenceEquals(_recipes, value))
                {
                    return;
                }

                DetachRecipes(_recipes);
                SetProperty(ref _recipes, value ?? new ObservableCollection<ProjectRecipeInfo>());
                AttachRecipes(_recipes);
                RaisePropertyChanged(nameof(RecipeCount));
                RaisePropertyChanged(nameof(CurrentRecipe));
            }
        }

        private Guid _currentRecipeId;
        public Guid CurrentRecipeId
        {
            get => _currentRecipeId;
            set
            {
                if (SetProperty(ref _currentRecipeId, value))
                {
                    RaisePropertyChanged(nameof(CurrentRecipe));
                }
            }
        }

        private DateTime _lastUpdatedAt = DateTime.Now;
        public DateTime LastUpdatedAt
        {
            get => _lastUpdatedAt;
            set => SetProperty(ref _lastUpdatedAt, value);
        }

        [JsonIgnore]
        public int RecipeCount => Recipes?.Count ?? 0;

        [JsonIgnore]
        public ProjectRecipeInfo CurrentRecipe => Recipes?.FirstOrDefault(item => item.Id == CurrentRecipeId);

        public void EnsureValidState()
        {
            Recipes ??= new ObservableCollection<ProjectRecipeInfo>();

            if (Recipes.Count == 0)
            {
                CurrentRecipeId = Guid.Empty;
                LastUpdatedAt = LastUpdatedAt == default ? DateTime.Now : LastUpdatedAt;
                return;
            }

            if (!Recipes.Any(item => item.Id == CurrentRecipeId))
            {
                CurrentRecipeId = Recipes[0].Id;
            }

            foreach (ProjectRecipeInfo recipe in Recipes)
            {
                recipe.IsCurrent = recipe.Id == CurrentRecipeId;
                recipe.Groups ??= new ObservableCollection<ProjectRecipeNodeGroup>();
                foreach (ProjectRecipeNodeGroup invalidGroup in recipe.Groups
                    .Where(group => group == null || group.Serial < 0)
                    .ToList())
                {
                    recipe.Groups.Remove(invalidGroup);
                }

                foreach (ProjectRecipeNodeGroup group in recipe.Groups)
                {
                    foreach (RecipeParamInfo parameter in group.Parameters.Where(item => item != null))
                    {
                        if (!parameter.IsEnable && parameter.IsRequired)
                        {
                            parameter.IsEnable = true;
                            parameter.IsRequired = false;
                        }
                    }
                }
            }

            LastUpdatedAt = LastUpdatedAt == default ? DateTime.Now : LastUpdatedAt;
            RaisePropertyChanged(nameof(RecipeCount));
            RaisePropertyChanged(nameof(CurrentRecipe));
        }

        public void SetCurrentRecipe(Guid recipeId)
        {
            CurrentRecipeId = recipeId;

            foreach (ProjectRecipeInfo recipe in Recipes)
            {
                recipe.IsCurrent = recipe.Id == recipeId;
            }

            RaisePropertyChanged(nameof(CurrentRecipe));
        }

        public ProjectRecipeConfig CreateSnapshot()
        {
            ProjectRecipeConfig snapshot = new()
            {
                CurrentRecipeId = CurrentRecipeId,
                LastUpdatedAt = LastUpdatedAt
            };

            foreach (ProjectRecipeInfo recipe in Recipes?.Where(item => item != null) ?? Enumerable.Empty<ProjectRecipeInfo>())
            {
                snapshot.Recipes.Add(recipe.CreateSnapshot());
            }

            snapshot.EnsureValidState();
            return snapshot;
        }

        private void AttachRecipes(ObservableCollection<ProjectRecipeInfo> recipes)
        {
            if (recipes == null)
            {
                return;
            }

            recipes.CollectionChanged -= Recipes_CollectionChanged;
            recipes.CollectionChanged += Recipes_CollectionChanged;
        }

        private void DetachRecipes(ObservableCollection<ProjectRecipeInfo> recipes)
        {
            if (recipes == null)
            {
                return;
            }

            recipes.CollectionChanged -= Recipes_CollectionChanged;
        }

        private void Recipes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(RecipeCount));
            RaisePropertyChanged(nameof(CurrentRecipe));
        }
    }
}
