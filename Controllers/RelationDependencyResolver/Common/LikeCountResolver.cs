using API.Models.Architecture;

namespace API.RelationDependencyResolver.Common {
    public class LikeCountResolver : RelationCounterResolver {
        public override string GetPropertyName () => nameof (InteractiveVisualDescriptiveModel.LikeCount);

        public override string GetRelationName () => "Like";
    }
}