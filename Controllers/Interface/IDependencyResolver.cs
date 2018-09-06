using Microsoft.EntityFrameworkCore;

namespace API.Interface {
    public interface IDependencyResolver {
        dynamic Resolve (
            DbContext dbContext,
            object engineService,
            string requesterId,
            object currentModel,
            string DependentOn
        );
    }
}