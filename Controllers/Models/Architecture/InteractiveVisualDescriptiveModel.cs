using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API.Attributes;
using API.DependencyResolver;
using API.Engine;
using API.Enums;
using API.Interface;
using API.Models.Temporary;
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

    class UserBookmarkedMeResolver : UserHasRelationDependencyResolver {
        public override string GetRelationName () {
            return "Bookmark";
        }
    }

    class UserLikedMeResolver : UserHasRelationDependencyResolver {
        public override string GetRelationName () {
            return "Like";
        }
    }

    class BookmarkCountResolver : IRelationDependent<Object> {
        public dynamic OnRelationEvent (
            DbContext dbContext,
            object model,
            string requesterID,
            IRequest request,
            IRequest dependentRequest,
            Object intractionType,
            HttpRequestMethod httpRequestMethod) {
            var intractionName = intractionType.ToString ();

            if (intractionName != "Bookmark")
                return null;

            if (httpRequestMethod != HttpRequestMethod.Post &&
                httpRequestMethod != HttpRequestMethod.Delete)
                return null;

            if (model == null)
                model = APIUtils.GetResource (dbContext, request) as object;
            if (model == null)
                return null;

            var bookmarkCountProp = model.GetType ().GetProperty (nameof (InteractiveVisualDescriptiveModel.BookmarkCount));
            var bookmarkCountValue = (long) bookmarkCountProp.GetValue (model);

            if (httpRequestMethod == HttpRequestMethod.Post) {
                bookmarkCountProp.SetValue (model, bookmarkCountValue + 1);
            } else if (httpRequestMethod == HttpRequestMethod.Delete) {
                bookmarkCountProp.SetValue (model, Math.Max (0, bookmarkCountValue - 1));
            }

            return model;
        }
    }

    class LikeCountResolver : IRelationDependent<Object> {
        public dynamic OnRelationEvent (
            DbContext dbContext,
            object model,
            string requesterID,
            IRequest request,
            IRequest dependentRequest,
            Object intractionType,
            HttpRequestMethod httpRequestMethod) {
            var intractionName = intractionType.ToString ();

            if (intractionName != "Like")
                return null;

            if (httpRequestMethod != HttpRequestMethod.Post &&
                httpRequestMethod != HttpRequestMethod.Delete)
                return null;

            if (model == null)
                model = APIUtils.GetResource (dbContext, request) as object;
            if (model == null)
                return null;

            var likeCountProp = model.GetType ().GetProperty (nameof (InteractiveVisualDescriptiveModel.LikeCount));
            var likeCountValue = (long) likeCountProp.GetValue (model);

            if (httpRequestMethod == HttpRequestMethod.Post) {
                likeCountProp.SetValue (model, likeCountValue + 1);
            } else if (httpRequestMethod == HttpRequestMethod.Delete) {
                likeCountProp.SetValue (model, Math.Max (0, likeCountValue - 1));
            }

            return model;
        }
    }
}