namespace API.DependencyResolver.Common {
    public sealed class UserBookmarkedMeResolver : UserHasRelationDependencyResolver {
        public override string GetRelationName () {
            return "Bookmark";
        }
    }
}