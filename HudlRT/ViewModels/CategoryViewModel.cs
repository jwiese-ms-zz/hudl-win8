﻿using Caliburn.Micro;
using HudlRT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HudlRT.ViewModels
{
    /// <summary>
    /// Used for binding to a list of categories
    /// </summary>
    public class CategoryViewModel : PropertyChangedBase
    {
        private string _name { get; set; }
        //private BindableCollection<Category> _cutups { get; set; }
        private long _categoryId { get; set; }

        public static CategoryViewModel FromDTO(CategoryDTO catDTO)
        {
            CategoryViewModel cat = new CategoryViewModel();
            cat._name = catDTO.Name;
            cat._categoryId = catDTO.CategoryId;
            return cat;
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                NotifyOfPropertyChange(() => Name);
            }
        }

        public long CategoryId
        {
            get { return _categoryId; }
            set
            {
                if (value == _categoryId) return;
                _categoryId = value;
                NotifyOfPropertyChange(() => CategoryId);
            }
        }
    }
}
