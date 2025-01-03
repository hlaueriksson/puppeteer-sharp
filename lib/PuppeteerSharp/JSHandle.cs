using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PuppeteerSharp.Cdp.Messaging;

namespace PuppeteerSharp
{
    /// <inheritdoc/>
    public abstract class JSHandle : IJSHandle
    {
        internal JSHandle()
        {
        }

        /// <inheritdoc/>
        public bool Disposed { get; protected set; }

        /// <inheritdoc/>
        public abstract RemoteObject RemoteObject { get; }

        internal Func<Task> DisposeAction { get; set; }

        internal abstract IsolatedWorld Realm { get; }

        internal Frame Frame => Realm.Environment as Frame;

        internal string Id => RemoteObject.ObjectId;

        /// <inheritdoc/>
        public virtual Task<IJSHandle> GetPropertyAsync(string propertyName)
            => EvaluateFunctionHandleAsync(
                @"(object, propertyName) => {
                        return object[propertyName];
                    }",
                propertyName);

        /// <inheritdoc/>
        public virtual async Task<Dictionary<string, IJSHandle>> GetPropertiesAsync()
        {
            var propertyNames = await EvaluateFunctionAsync<string[]>(@"object => {
                    const enumerableProperties = [];
                    const descriptors = Object.getOwnPropertyDescriptors(object);
                    for (const propertyName in descriptors) {
                        if (descriptors[propertyName]?.enumerable)
                        {
                            enumerableProperties.push(propertyName);
                        }
                    }
                    return enumerableProperties;
                }").ConfigureAwait(false);

            var dic = new Dictionary<string, IJSHandle>();

            foreach (var key in propertyNames)
            {
                var handleItem = await GetPropertyAsync(key).ConfigureAwait(false);
                if (handleItem is not null)
                {
                    dic.Add(key, handleItem);
                }
            }

            return dic;
        }

        /// <inheritdoc/>
        public async Task<object> JsonValueAsync() => await JsonValueAsync<object>().ConfigureAwait(false);

        /// <inheritdoc/>
        public abstract Task<T> JsonValueAsync<T>();

        /// <inheritdoc/>
        public abstract ValueTask DisposeAsync();

        /// <inheritdoc/>
        public Task<IJSHandle> EvaluateFunctionHandleAsync(string pageFunction, params object[] args)
        {
            return Realm.EvaluateFunctionHandleAsync(pageFunction, [this, .. args]);
        }

        /// <inheritdoc/>
        public async Task<JsonElement?> EvaluateFunctionAsync(string script, params object[] args)
        {
            var adoptedThis = await Realm.AdoptHandleAsync(this).ConfigureAwait(false);
            return await Realm.EvaluateFunctionAsync<JsonElement?>(script, [adoptedThis, .. args])
                .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<T> EvaluateFunctionAsync<T>(string script, params object[] args)
        {
            return Realm.EvaluateFunctionAsync<T>(script, [this, .. args]);
        }
    }
}
