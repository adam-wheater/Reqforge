using RocketBoy.Components.Pages.Models;

namespace RocketBoy.Services
{
    /// <summary>
    /// Holds a shared list of RequestObject instances imported via OpenAPI.
    /// Components can read Requests and subscribe to OnChange to react.
    /// </summary>
    public class RequestStore
    {
        private readonly List<RequestObject> _requests = new();

        /// <summary>
        /// Read‐only view of the current requests.
        /// </summary>
        public IReadOnlyList<RequestObject> Requests => _requests;

        /// <summary>
        /// Fired whenever new requests are added.
        /// </summary>
        public event Action? OnChange;

        /// <summary>
        /// Add a batch of requests and notify subscribers.
        /// </summary>
        public void AddRange(IEnumerable<RequestObject> requests)
        {
            _requests.AddRange(requests);
            OnChange?.Invoke();
        }
    }
}
