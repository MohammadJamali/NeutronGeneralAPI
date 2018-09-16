using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API.Attributes;
using API.DependencyResolver;
using API.DependencyResolver.Common;
using API.Engine;
using API.Enums;
using API.Interface;
using API.Models.Temporary;
using API.RelationDependencyResolver;
using API.RelationDependencyResolver.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace API.Models.Architecture {

    public abstract class InteractiveVisualDescriptiveModel : VisualDescriptiveModel {
        [Editable (false)]
        [BindNever]
        [RelationDependentValue (typeof (LikeCountResolver), typeof (LikeCountResolver))]
        public long LikeCount { get; set; }

        [Editable (false)]
        [BindNever]
        [RelationDependentValue (typeof (BookmarkCountResolver), typeof (BookmarkCountResolver))]
        public long BookmarkCount { get; set; }

        [Editable (false)]
        [BindNever]
        [NotMapped]
        [DependentValue (HttpRequestMethod.Get, ModelAction.Read, typeof (UserBookmarkedMeResolver))]
        public bool Bookmarked { get; set; }

        [Editable (false)]
        [NotMapped]
        [BindNever]
        [DependentValue (HttpRequestMethod.Get, ModelAction.Read, typeof (UserLikedMeResolver))]
        public bool Liked { get; set; }
    }
}