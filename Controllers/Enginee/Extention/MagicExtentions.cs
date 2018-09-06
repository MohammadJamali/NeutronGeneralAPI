using System;
using Microsoft.EntityFrameworkCore;

namespace API.Engine.Extention {
    public static class MagicExtentions {
        /// <summary>
        /// This function will compensate vacuum of DbContext.Set(Type T)
        /// </summary>
        /// <param name="context"> DbContext </param>
        /// <param name="type"> Type </param>
        /// <returns> DbSet<Type> </returns>
        public static dynamic MagicDbSet (this DbContext context, Type type) =>
            typeof (MagicExtentions)
            .GetMethod ("GenericMagicDbSet")
            .MakeGenericMethod (type)
            .Invoke (null, new object[] { context });

        /// <summary>
        /// This function must be called by <see cref="MagicDbSet"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static DbSet<T> GenericMagicDbSet<T> (DbContext context) where T : class =>
            context.Set<T> ();

        /// <summary>
        /// This function will try to add ModelIntraction (which is a root class) as requested
        /// child class into DbSet<T> if present or DbContext.Set<T>() otherwise, with this function
        /// we can insert each ModelIntraction in its own table
        /// </summary>
        /// <param name="context">This object must be either DbContext or DbSet<T></param>
        /// <param name="intraction">This is root class (ModelIntraction<TRelation>)</param>
        /// <param name="instractionType">This is child class (Like, Bookmark, ...)</param>
        public static void MagicAddIntraction (this object context, object intraction, Type instractionType) =>
            typeof (MagicExtentions)
            .GetMethod ("MagicAdd")
            .MakeGenericMethod (instractionType)
            .Invoke (null, new object[] { context, intraction });

        /// <summary>
        /// This function must be called by <see cref="MagicAddIntraction"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void MagicAdd<T> (dynamic container, object entity) where T : class {
            if (container is DbContext)
                (container as DbContext).Set<T> ().Add (Activator.CreateInstance (typeof (T), new object[] { entity }) as T);
            else
                container.Add (Activator.CreateInstance (typeof (T), new object[] { entity }) as T);
        }
    }
}