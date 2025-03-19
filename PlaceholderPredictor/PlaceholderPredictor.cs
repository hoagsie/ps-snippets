using System.Management.Automation;
using System.Management.Automation.Subsystem.Prediction;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PlaceholderPredictor;

public class PlaceholderPredictor : ICommandPredictor
{
    private readonly string _filename;
    private readonly string _prefix;
    private readonly bool _prependPrevious;

    internal PlaceholderPredictor(string guid, string name, string description, string filename, string prefix, bool prependPrevious)
    {
        Name = name;
        Description = description;
        _filename = filename;
        _prefix = prefix;
        _prependPrevious = prependPrevious;
        Id = new Guid(guid);
    }

    /// <summary>
    /// Gets the unique identifier for a subsystem implementation.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the name of a subsystem implementation.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of a subsystem implementation.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Get the predictive suggestions. It indicates the start of a suggestion rendering session.
    /// </summary>
    /// <param name="client">Represents the client that initiates the call.</param>
    /// <param name="context">The <see cref="PredictionContext"/> object to be used for prediction.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the prediction.</param>
    /// <returns>An instance of <see cref="SuggestionPackage"/>.</returns>
    public SuggestionPackage GetSuggestion(PredictionClient client, PredictionContext context, CancellationToken cancellationToken)
    {
        var userInput = context.InputAst.Extent.Text;

        var trigger = Regex.Match(userInput, $"(.*?)({_prefix}:)(\\S*)$");
        
        if (!trigger.Success)
        {
            return default;
        }

        var partial = string.IsNullOrWhiteSpace(trigger.Groups["3"].Value)
            ? "*"
            : trigger.Groups["3"].Value + "*";

        var filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            _filename
        );

        var results = new List<PredictiveSuggestion>();

        try
        {
            var json = File.ReadAllText(filePath);
            var doc = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            var wc = new WildcardPattern(partial, WildcardOptions.IgnoreCase);

            foreach (var (key, value) in doc)
            {
                if (!wc.IsMatch(key)) continue;

                if (!string.IsNullOrEmpty(value))
                {
                    var suggestion = _prependPrevious
                        ? $"{trigger.Groups["1"].Value}{value}"
                        : value;

                    results.Add(new PredictiveSuggestion(suggestion, $"[{key}]"));
                }
            }
        }
        catch
        {
            // Swallow exceptions in predictor to avoid polluting the console
        }

        return results.Count > 0 ? new SuggestionPackage(results) : default;
    }

    #region "interface methods for processing feedback"

    /// <summary>
    /// Gets a value indicating whether the predictor accepts a specific kind of feedback.
    /// </summary>
    /// <param name="client">Represents the client that initiates the call.</param>
    /// <param name="feedback">A specific type of feedback.</param>
    /// <returns>True or false, to indicate whether the specific feedback is accepted.</returns>
    public bool CanAcceptFeedback(PredictionClient client, PredictorFeedbackKind feedback) => false;

    /// <summary>
    /// One or more suggestions provided by the predictor were displayed to the user.
    /// </summary>
    /// <param name="client">Represents the client that initiates the call.</param>
    /// <param name="session">The mini-session where the displayed suggestions came from.</param>
    /// <param name="countOrIndex">
    /// When the value is greater than 0, it's the number of displayed suggestions from the list
    /// returned in <paramref name="session"/>, starting from the index 0. When the value is
    /// less than or equal to 0, it means a single suggestion from the list got displayed, and
    /// the index is the absolute value.
    /// </param>
    public void OnSuggestionDisplayed(PredictionClient client, uint session, int countOrIndex) { }

    /// <summary>
    /// The suggestion provided by the predictor was accepted.
    /// </summary>
    /// <param name="client">Represents the client that initiates the call.</param>
    /// <param name="session">Represents the mini-session where the accepted suggestion came from.</param>
    /// <param name="acceptedSuggestion">The accepted suggestion text.</param>
    public void OnSuggestionAccepted(PredictionClient client, uint session, string acceptedSuggestion) { }

    /// <summary>
    /// A command line was accepted to execute.
    /// The predictor can start processing early as needed with the latest history.
    /// </summary>
    /// <param name="client">Represents the client that initiates the call.</param>
    /// <param name="history">History command lines provided as references for prediction.</param>
    public void OnCommandLineAccepted(PredictionClient client, IReadOnlyList<string> history) { }

    /// <summary>
    /// A command line was done execution.
    /// </summary>
    /// <param name="client">Represents the client that initiates the call.</param>
    /// <param name="commandLine">The last accepted command line.</param>
    /// <param name="success">Shows whether the execution was successful.</param>
    public void OnCommandLineExecuted(PredictionClient client, string commandLine, bool success) { }

    #endregion;
}