using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using API.Interface;
using API.Models;
using API.Models.Architecture;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Engine.Extention {
    public static class DatabaseExtention {

        /// <summary>
        /// This function must be called in <see cref="IdentityDbContext<TUser> "/> class and in
        /// <see cref="OnModelCreating"/> function after <code>base.OnModelCreating (builder);</code>
        ///
        /// This function will try to create table per each relation which has been determined with
        /// TRelation enum type with "IntractionTable" postfix
        ///
        /// </summary>
        /// <param name="builder"> Microsoft.EntityFrameworkCore.ModelBuilder </param>
        /// <param name="engineService"> This can be an instance of any class which has
        /// implemented <seealso IApiEngineService<TRelation, TUser>> interface </param>
        /// <param name="TPH"> Table Per Hierarchy </param>
        /// <typeparam name="TRelation"> Type of relation enum </typeparam>
        /// <typeparam name="TUser"> Type of user class which is extended from <see cref="IdentityUser"/></typeparam>
        /// <returns></returns>
        public static void ConfigureAPIDatabase<TRelation, TUser> (
            this ModelBuilder builder,
            IApiEngineService<TRelation, TUser> engineService,
            bool TPH = true) where TUser : IdentityUser {

            if (TPH) {
                builder.Ignore<RootModel> ();
                builder.Ignore<DescriptiveModel> ();
                builder.Ignore<VisualDescriptiveModel> ();
                builder.Ignore<ModelIntraction<TRelation>> ();
            }

            var relations = Enum.GetNames (typeof (TRelation));

            foreach (var relation in relations) {
                var entity = builder.Entity (engineService.MapRelationToType (relation));
                entity.HasKey (new string[] {
                    "FirstModelId",
                    "SecondModelId",
                    "CreateDateTime",
                    "IntractionType"
                });
                entity.ToTable (relation + "IntractionTable");
            }
        }
    }
}