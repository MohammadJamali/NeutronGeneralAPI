using API.Models.Architecture;

namespace API.RelationDependencyResolver.Common {
    public sealed class BookmarkCountResolver : RelationCounterResolver {
        public override string GetPropertyName () => nameof (InteractiveVisualDescriptiveModel.BookmarkCount);

        public override string GetRelationName () => "Bookmark";
    }
}