using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace PaZword.Api.Models
{
    /// <summary>
    /// Represents a category
    /// </summary>
    public sealed class Category : INotifyPropertyChanged, IExactEquatable<Category>
    {
        private string _name;
        private CategoryIcon _categoryIcon;

        /// <summary>
        /// Gets the category's unique ID.
        /// </summary>
        [JsonProperty]
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the category's name.
        /// </summary>
        [JsonProperty]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        /// <summary>
        /// Gets or sets the category's icon.
        /// </summary>
        [JsonProperty]
        public CategoryIcon Icon
        {
            get => _categoryIcon;
            set
            {
                _categoryIcon = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
            }
        }

        /// <summary>
        /// Gets or sets when is the last time this category has changed.
        /// </summary>
        [JsonProperty]
        public DateTime LastModificationDate { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initialize a new instance of the <see cref="Category"/> class.
        /// </summary>
        public Category()
        {
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="Category"/> class.
        /// </summary>
        /// <param name="id">The unique ID of the category</param>
        /// <param name="name">The category name</param>
        public Category(Guid id, string name, CategoryIcon icon)
        {
            Id = id;
            Name = name;
            Icon = icon;
        }

        public override bool Equals(object obj)
        {
            if (obj is Category category)
            {
                return Equals(category);
            }

            return false;
        }

        public bool Equals(Category other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return ReferenceEquals(this, other)
                || other.Id == Id;
        }

        public bool ExactEquals(Category other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return ReferenceEquals(this, other)
                || (other.Id == Id
                    && other.LastModificationDate == LastModificationDate
                    && string.Equals(other.Name, Name, StringComparison.Ordinal))
                    && other.Icon == Icon;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ 137;
        }

        public static bool operator ==(Category left, Category right)
        {
            if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            return left.Equals(right);
        }
        public static bool operator !=(Category left, Category right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
