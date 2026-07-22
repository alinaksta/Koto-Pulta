using Game.Characters;

namespace Game.Progression
{
    /// <summary>
    /// Provides shared services to the active game mode.
    /// </summary>
    public sealed class GameModeContext
    {
        /// <summary>
        /// Creates a new game mode context from the supplied runtime services.
        /// </summary>
        public GameModeContext(GameModeService gameModes, RunSessionService session, BalanceService balance, CustomerService customers, WaiterService waiters)
        {
            GameModes = gameModes;
            Session = session;
            Balance = balance;
            Customers = customers;
            Waiters = waiters;
        }

        /// <summary>
        /// Gets the game mode service.
        /// </summary>
        public GameModeService GameModes { get; }
        /// <summary>
        /// Gets the run session service.
        /// </summary>
        public RunSessionService Session { get; }
        /// <summary>
        /// Gets the balance service.
        /// </summary>
        public BalanceService Balance { get; }
        /// <summary>
        /// Gets the customer service.
        /// </summary>
        public CustomerService Customers { get; }
        /// <summary>
        /// Gets the waiter service.
        /// </summary>
        public WaiterService Waiters { get; }
    }

    /// <summary>
    /// Defines the lifecycle for a runtime game mode.
    /// </summary>
    public interface IGameMode
    {
        /// <summary>
        /// Gets the stable identifier for this game mode.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Checks whether this mode can start with the supplied context.
        /// </summary>
        /// <param name="context">Shared runtime services for the mode.</param>
        /// <returns><see langword="true"/> when the mode can start.</returns>
        bool CanStart(GameModeContext context);

        /// <summary>
        /// Activates the game mode.
        /// </summary>
        /// <param name="context">Shared runtime services for the mode.</param>
        void Enter(GameModeContext context);

        /// <summary>
        /// Advances the game mode by one frame.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        void Tick(float deltaTime);

        /// <summary>
        /// Deactivates the game mode.
        /// </summary>
        void Exit();
    }
}
