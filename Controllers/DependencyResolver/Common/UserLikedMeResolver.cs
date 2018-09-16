namespace API.DependencyResolver.Common {

    public sealed class UserLikedMeResolver : UserHasRelationDependencyResolver {
        public override string GetRelationName () {
            return "Like";
        }
    }
}