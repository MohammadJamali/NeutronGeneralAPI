using API.Interface;
using Microsoft.EntityFrameworkCore;

namespace API.DependencyResolver {
    public class JustCopyDependencyResolver : IDependencyResolver {
        public dynamic Resolve (
            DbContext dbContext,
            object engineService,
            string requesterId,
            object currentModel,
            string DependentOn) {
            return currentModel;
        }
    }
}