namespace Privileged;

public class AuthorizationContext(IReadOnlyCollection<AuthorizationRule> rules, StringComparer? stringComparer = null)
{
    public IReadOnlyCollection<AuthorizationRule> Rules { get; } = rules ?? throw new ArgumentNullException(nameof(rules));

    public StringComparer StringComparer { get; } = stringComparer ?? StringComparer.InvariantCultureIgnoreCase;


    public bool Authorized(string? action, string? subject, string? field = null)
    {
        if (action is null || subject is null)
            return false;

        var matchedRules = MatchRules(action, subject, field);
        bool? state = null;

        foreach (var matchedRule in matchedRules)
        {
            if (matchedRule.Denied == true)
                state = state != null && (state.Value && false);
            else
                state = state == null || (state.Value || true);
        }

        return state ?? false;
    }

    public bool Unauthorized(string? action, string? subject, string? field = null) => !Authorized(action, subject, field);

    public IEnumerable<AuthorizationRule> MatchRules(string? action, string? subject, string? field = null)
    {
        if (action is null || subject is null)
            return Enumerable.Empty<AuthorizationRule>();

        return Rules.Where(r => RuleMatcher(r, action, subject, field));
    }


    private bool RuleMatcher(AuthorizationRule rule, string action, string subject, string? field = null)
    {
        return SubjectMather(rule, subject)
               && ActionMather(rule, action)
               && FieldMatcher(rule, field);
    }

    private bool SubjectMather(AuthorizationRule rule, string subject)
    {
        // can match global all or requested subject
        return StringComparer.Equals(rule.Subject, subject)
               || StringComparer.Equals(rule.Subject, AuthorizationSubjects.All);
    }

    private bool ActionMather(AuthorizationRule rule, string action)
    {
        // can match global manage action or requested action
        return StringComparer.Equals(rule.Action, action)
               || StringComparer.Equals(rule.Action, AuthorizationActions.All);
    }

    private bool FieldMatcher(AuthorizationRule rule, string? field)
    {
        // if rule doesn't have fields, all allowed
        if (field == null || rule.Fields == null || rule.Fields.Count == 0)
            return true;

        // ensure rule has field
        return rule.Fields.Contains(field, StringComparer);
    }
}
