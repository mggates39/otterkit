using static Otterkit.Types.TokenHandling;
using Otterkit.Types;

namespace Otterkit.Analyzers;

public struct SetLcValues
{
    public bool LC_ALL;
    public bool LC_COLLATE;
    public bool LC_CTYPE;
    public bool LC_MESSAGES;
    public bool LC_MONETARY;
    public bool LC_NUMERIC;
    public bool LC_TIME;
}

public static partial class Common
{
    // The following methods are responsible for parsing some commonly repeated pieces of COBOL statements.
    // The ON SIZE ERROR, ON EXCEPTION, INVALID KEY, AT END, and the RETRY phrase are examples of pieces of COBOL syntax
    // that appear on multiple statements. Reusing the same code in those cases keeps things much more modular and easier to maintain.
    //
    // The Arithmetic() and Condition() methods are responsible for parsing expressions and verifying if those expressions were
    // written correctly. This is using a combination of the Shunting Yard algorithm, and some methods to verify if the 
    // parentheses are balanced and if it can be evaluated correctly.

    public static void AscendingDescendingKey()
    {
        while (CurrentEquals("ASCENDING DESCENDING"))
        {
            Choice("ASCENDING DESCENDING");
            Optional("KEY");
            Optional("IS");

            References.Identifier();
        }
    }

    public static void TimesPhrase()
    {
        if (CurrentEquals(TokenType.Identifier))
        {
            References.Identifier();
        }
        else
        {
            Literals.Numeric();
        }

        Expected("TIMES");
    }

    public static void UntilPhrase()
    {
        Expected("UNTIL");
        if (CurrentEquals("EXIT"))
        {
            Expected("EXIT");
        }
        else
        {
            Condition(" ");
        }
    }

    public static void VaryingPhrase()
    {
        Expected("VARYING");
        References.Identifier();
        Expected("FROM");
        if (CurrentEquals(TokenType.Numeric))
        {
            Literals.Numeric();
        }
        else
        {
            References.Identifier();
        }

        if (CurrentEquals("BY"))
        {
            Expected("BY");
            if (CurrentEquals(TokenType.Numeric))
            {
                Literals.Numeric();
            }
            else
            {
                References.Identifier();
            }
        }

        Expected("UNTIL");
        Condition("AFTER");

        while (CurrentEquals("AFTER"))
        {
            Expected("AFTER");
            References.Identifier();
            Expected("FROM");
            if (CurrentEquals(TokenType.Numeric))
            {
                Literals.Numeric();
            }
            else
            {
                References.Identifier();
            }

            if (CurrentEquals("BY"))
            {
                Expected("BY");
                if (CurrentEquals(TokenType.Numeric))
                {
                    Literals.Numeric();
                }
                else
                {
                    References.Identifier();
                }
            }

            Expected("UNTIL");
            Condition("AFTER");
        }
    }

    public static void WithTest()
    {
        if (CurrentEquals("WITH TEST"))
        {
            Optional("WITH");
            Expected("TEST");
            Choice("BEFORE AFTER");
        }
    }

    public static void RetryPhrase()
    {
        if (!CurrentEquals("RETRY")) return;
        
        var hasFor = false;

        Expected("RETRY");
        if (CurrentEquals("FOREVER"))
        {
            Expected("FOREVER");
            return;
        }

        if (CurrentEquals("FOR"))
        {
            Optional("FOR");
            hasFor = true;
        }

        Arithmetic("SECONDS TIMES");
        if (CurrentEquals("SECONDS") || hasFor)
        {
            Expected("SECONDS");
        }
        else
        {
            Expected("TIMES");
        }
    }

    public static void TallyingPhrase()
    {
        if (!CurrentEquals(TokenType.Identifier) && !PeekEquals(1, "FOR"))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                Tallying phrase, missing identifier.
                """)
            .WithSourceLine(Current(), """
                Tallying must start with an identifier, followed by the 'FOR' keyword.
                """)
            .CloseError();
        }

        while (CurrentEquals(TokenType.Identifier) && PeekEquals(1, "FOR"))
        {
            References.Identifier();
            Expected("FOR");

            if (!CurrentEquals("CHARACTERS ALL LEADING"))
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Tallying phrase, missing keyword.
                    """)
                .WithSourceLine(Current(), """
                    Missing phrase keyword.
                    """)
                .WithNote("""
                    Tallying must contain one of the following words: CHARACTERS, ALL or LEADING
                    """)
                .CloseError();
            }

            while (CurrentEquals("CHARACTERS ALL LEADING"))
            {
                if (CurrentEquals("CHARACTERS"))
                {
                    Expected("CHARACTERS");
                    if (CurrentEquals("AFTER BEFORE"))
                    {
                        AfterBeforePhrase();
                    }
                }
                else if (CurrentEquals("ALL"))
                {
                    Expected("ALL");
                    if (CurrentEquals(TokenType.Identifier))
                    {
                        References.Identifier();
                    }
                    else
                    {
                        Literals.String();
                    }

                    if (CurrentEquals("AFTER BEFORE"))
                    {
                        AfterBeforePhrase();
                    }

                    while (CurrentEquals(TokenType.Identifier | TokenType.String))
                    {
                        if (CurrentEquals(TokenType.Identifier))
                        {
                            References.Identifier();
                        }
                        else
                        {
                            Literals.String();
                        }

                        if (CurrentEquals("AFTER BEFORE"))
                        {
                            AfterBeforePhrase();
                        }
                    }
                }
                else if (CurrentEquals("LEADING"))
                {
                    Expected("LEADING");
                    if (CurrentEquals(TokenType.Identifier))
                    {
                        References.Identifier();
                    }
                    else
                    {
                        Literals.String();
                    }

                    if (CurrentEquals("AFTER BEFORE"))
                    {
                        AfterBeforePhrase();
                    }

                    while (CurrentEquals(TokenType.Identifier | TokenType.String))
                    {
                        if (CurrentEquals(TokenType.Identifier))
                        {
                            References.Identifier();
                        }
                        else
                        {
                            Literals.String();
                        }

                        if (CurrentEquals("AFTER BEFORE"))
                        {
                            AfterBeforePhrase();
                        }
                    }
                }
            }
        }
    }

    public static void ReplacingPhrase()
    {
        if (!CurrentEquals("CHARACTERS ALL LEADING FIRST"))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                Replacing phrase, missing keyword.
                """)
            .WithSourceLine(Current(), """
                Missing phrase keyword.
                """)
            .WithNote("""
                Tallying must contain one of the following words: CHARACTERS, ALL, LEADING or FIRST.
                """)
            .CloseError();
        }

        while (CurrentEquals("CHARACTERS ALL LEADING FIRST"))
        {
            if (CurrentEquals("CHARACTERS"))
            {
                Expected("CHARACTERS");
                Expected("BY");

                if (CurrentEquals(TokenType.Identifier))
                {
                    References.Identifier();
                }
                else
                {
                    Literals.String();
                }

                if (CurrentEquals("AFTER BEFORE"))
                {
                    AfterBeforePhrase();
                }
            }
            else if (CurrentEquals("ALL"))
            {
                Expected("ALL");
                if (CurrentEquals(TokenType.Identifier))
                {
                    References.Identifier();
                }
                else
                {
                    Literals.String();
                }

                Expected("BY");
                if (CurrentEquals(TokenType.Identifier))
                {
                    References.Identifier();
                }
                else
                {
                    Literals.String();
                }

                if (CurrentEquals("AFTER BEFORE"))
                {
                    AfterBeforePhrase();
                }

                while (CurrentEquals(TokenType.Identifier | TokenType.String))
                {
                    if (CurrentEquals(TokenType.Identifier))
                    {
                        References.Identifier();
                    }
                    else
                    {
                        Literals.String();
                    }

                    Expected("BY");
                    if (CurrentEquals(TokenType.Identifier))
                    {
                        References.Identifier();
                    }
                    else
                    {
                        Literals.String();
                    }

                    if (CurrentEquals("AFTER BEFORE"))
                    {
                        AfterBeforePhrase();
                    }

                    if (CurrentEquals("AFTER BEFORE"))
                    {
                        AfterBeforePhrase();
                    }
                }
            }
            else if (CurrentEquals("LEADING"))
            {
                Expected("LEADING");
                if (CurrentEquals(TokenType.Identifier))
                {
                    References.Identifier();
                }
                else
                {
                    Literals.String();
                }

                Expected("BY");
                if (CurrentEquals(TokenType.Identifier))
                {
                    References.Identifier();
                }
                else
                {
                    Literals.String();
                }

                if (CurrentEquals("AFTER BEFORE"))
                {
                    AfterBeforePhrase();
                }

                if (CurrentEquals("AFTER BEFORE"))
                {
                    AfterBeforePhrase();
                }

                while (CurrentEquals(TokenType.Identifier | TokenType.String))
                {
                    if (CurrentEquals(TokenType.Identifier))
                    {
                        References.Identifier();
                    }
                    else
                    {
                        Literals.String();
                    }

                    Expected("BY");
                    if (CurrentEquals(TokenType.Identifier))
                    {
                        References.Identifier();
                    }
                    else
                    {
                        Literals.String();
                    }

                    if (CurrentEquals("AFTER BEFORE"))
                    {
                        AfterBeforePhrase();
                    }

                    if (CurrentEquals("AFTER BEFORE"))
                    {
                        AfterBeforePhrase();
                    }
                }
            }
            else if (CurrentEquals("FIRST"))
            {
                Expected("FIRST");
                if (CurrentEquals(TokenType.Identifier))
                {
                    References.Identifier();
                }
                else
                {
                    Literals.String();
                }

                Expected("BY");
                if (CurrentEquals(TokenType.Identifier))
                {
                    References.Identifier();
                }
                else
                {
                    Literals.String();
                }

                if (CurrentEquals("AFTER BEFORE"))
                {
                    AfterBeforePhrase();
                }

                if (CurrentEquals("AFTER BEFORE"))
                {
                    AfterBeforePhrase();
                }

                while (CurrentEquals(TokenType.Identifier | TokenType.String))
                {
                    if (CurrentEquals(TokenType.Identifier))
                    {
                        References.Identifier();
                    }
                    else
                    {
                        Literals.String();
                    }

                    Expected("BY");
                    if (CurrentEquals(TokenType.Identifier))
                    {
                        References.Identifier();
                    }
                    else
                    {
                        Literals.String();
                    }

                    if (CurrentEquals("AFTER BEFORE"))
                    {
                        AfterBeforePhrase();
                    }

                    if (CurrentEquals("AFTER BEFORE"))
                    {
                        AfterBeforePhrase();
                    }
                }
            }
        }
    }

    public static void AfterBeforePhrase(bool beforeExists = false, bool afterExists = false)
    {
        if (CurrentEquals("AFTER"))
        {
            if (afterExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    After phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    AFTER can only be specified once in this part of the statement.
                    """)
                .WithNote("""
                    The same applies to BEFORE.
                    """)
                .CloseError();
            }

            afterExists = true;
            Expected("AFTER");
            Optional("INITIAL");

            if (CurrentEquals(TokenType.Identifier))
            {
                References.Identifier();
            }
            else
            {
                Literals.String();
            }

            AfterBeforePhrase(beforeExists, afterExists);

        }

        if (CurrentEquals("BEFORE"))
        {
            if (beforeExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Before phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    BEFORE can only be specified once in this part of the statement.
                    """)
                .WithNote("""
                    The same applies to AFTER.
                    """)
                .CloseError();
            }

            beforeExists = true;
            Expected("BEFORE");
            Optional("INITIAL");

            if (CurrentEquals(TokenType.Identifier))
            {
                References.Identifier();
            }
            else
            {
                Literals.String();
            }

            AfterBeforePhrase(beforeExists, afterExists);
        }
    }

    public static void InvalidKey(ref bool isConditional, bool invalidKeyExists = false, bool notInvalidKeyExists = false)
    {
        if (CurrentEquals("INVALID"))
        {
            if (invalidKeyExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Invalid key phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    INVALID KEY can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to NOT INVALID KEY.
                    """)
                .CloseError();
            }
            isConditional = true;
            invalidKeyExists = true;

            Expected("INVALID");
            Optional("KEY");

            Statements.WithoutSections(true);

            InvalidKey(ref isConditional, invalidKeyExists, notInvalidKeyExists);
        }

        if (CurrentEquals("NOT"))
        {
            if (notInvalidKeyExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Not invalid key phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    NOT INVALID KEY can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to INVALID KEY.
                    """)
                .CloseError();
            }
            isConditional = true;
            notInvalidKeyExists = true;

            Expected("NOT");
            Expected("INVALID");
            Optional("KEY");

            Statements.WithoutSections(true);

            InvalidKey(ref isConditional, invalidKeyExists, notInvalidKeyExists);
        }
    }

    public static void OnException(ref bool isConditional, bool onExceptionExists = false, bool notOnExceptionExists = false)
    {
        if (CurrentEquals("ON") || CurrentEquals("EXCEPTION"))
        {
            if (onExceptionExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    On exception phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    ON EXCEPTION can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to NOT ON EXCEPTION.
                    """)
                .CloseError();
            }
            isConditional = true;
            onExceptionExists = true;

            Optional("ON");
            Expected("EXCEPTION");

            Statements.WithoutSections(true);

            OnException(ref isConditional, onExceptionExists, notOnExceptionExists);
        }

        if (CurrentEquals("NOT"))
        {
            if (notOnExceptionExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Not on exception phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    NOT ON EXCEPTION can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to ON EXCEPTION.
                    """)
                .CloseError();
            }
            isConditional = true;
            notOnExceptionExists = true;

            Expected("NOT");
            Optional("ON");
            Expected("EXCEPTION");

            Statements.WithoutSections(true);

            OnException(ref isConditional, onExceptionExists, notOnExceptionExists);
        }
    }

    public static void RaisingStatus(bool raisingExists = false, bool statusExists = false)
    {
        if (CurrentEquals("RAISING"))
        {
            if (raisingExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Raising phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    Raising can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to WITH ... ERROR STATUS.
                    """)
                .CloseError();
            }

            Expected("RAISING");
            if (CurrentEquals("EXCEPTION"))
            {
                Expected("EXCEPTION");
                References.Identifier();
            }
            else if (CurrentEquals("LAST"))
            {
                Expected("LAST");
                Optional("EXCEPTION");
            }
            else
                References.Identifier();

            raisingExists = true;
            RaisingStatus(raisingExists, statusExists);

        }

        if (CurrentEquals("WITH") || CurrentEquals("NORMAL") || CurrentEquals("ERROR"))
        {
            if (statusExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Error status phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    ERROR STATUS can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to RAISING.
                    """)
                .CloseError();
            }

            Optional("WITH");
            Choice("NORMAL ERROR");
            Optional("STATUS");
            switch (Current().Type)
            {
                case TokenType.Identifier:
                    References.Identifier();
                    break;
                case TokenType.Numeric:
                    Literals.Numeric();
                    break;
                case TokenType.String:
                    Literals.String();
                    break;
            }

            statusExists = true;
            RaisingStatus(raisingExists, statusExists);
        }
    }

    public static void AtEnd(ref bool isConditional, bool atEndExists = false, bool notAtEndExists = false)
    {
        if (CurrentEquals("AT") || CurrentEquals("END"))
        {
            if (atEndExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    At end phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    AT END can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to NOT AT END.
                    """)
                .CloseError();
            }
            isConditional = true;
            atEndExists = true;

            Optional("AT");
            Expected("END");

            Statements.WithoutSections(true);

            AtEnd(ref isConditional, atEndExists, notAtEndExists);
        }

        if (CurrentEquals("NOT"))
        {
            if (notAtEndExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Not at end phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    NOT AT END can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to AT END.
                    """)
                .CloseError();
            }
            isConditional = true;
            notAtEndExists = true;

            Expected("NOT");
            Optional("AT");
            Expected("END");

            Statements.WithoutSections(true);

            AtEnd(ref isConditional, atEndExists, notAtEndExists);
        }
    }

    public static void SizeError(ref bool isConditional, bool onErrorExists = false, bool notOnErrorExists = false)
    {
        if (CurrentEquals("ON") || CurrentEquals("SIZE"))
        {
            if (onErrorExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    On size error phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    ON SIZE ERROR can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to NOT ON SIZE ERROR.
                    """)
                .CloseError();
            }
            isConditional = true;
            onErrorExists = true;

            Optional("ON");
            Expected("SIZE");
            Expected("ERROR");

            Statements.WithoutSections(true);

            SizeError(ref isConditional, onErrorExists, notOnErrorExists);
        }

        if (CurrentEquals("NOT"))
        {
            if (notOnErrorExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Not on size error phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    NOT ON SIZE ERROR can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to ON SIZE ERROR.
                    """)
                .CloseError();
            }
            isConditional = true;
            notOnErrorExists = true;

            Expected("NOT");
            Optional("ON");
            Expected("SIZE");
            Expected("ERROR");

            Statements.WithoutSections(true);

            SizeError(ref isConditional, onErrorExists, notOnErrorExists);
        }
    }

    public static void OnOverflow(ref bool isConditional, bool onOverflowExists = false, bool notOnOverflowExists = false)
    {
        if (CurrentEquals("ON") || CurrentEquals("OVERFLOW"))
        {
            if (onOverflowExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    On overflow phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    ON OVERFLOW can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to NOT ON OVERFLOW.
                    """)
                .CloseError();
            }
            isConditional = true;
            onOverflowExists = true;

            Optional("ON");
            Expected("OVERFLOW");

            Statements.WithoutSections(true);

            OnOverflow(ref isConditional, onOverflowExists, notOnOverflowExists);
        }

        if (CurrentEquals("NOT"))
        {
            if (notOnOverflowExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Not on overflow phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    NOT ON OVERFLOW can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to ON OVERFLOW.
                    """)
                .CloseError();
            }
            isConditional = true;
            notOnOverflowExists = true;

            Expected("NOT");
            Optional("ON");
            Expected("OVERFLOW");

            Statements.WithoutSections(true);

            OnOverflow(ref isConditional, onOverflowExists, notOnOverflowExists);
        }
    }

    public static void WriteBeforeAfter(bool beforeExists = false, bool afterExists = false)
    {
        if (CurrentEquals("BEFORE"))
        {
            if (beforeExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Before phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    BEFORE can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to AFTER.
                    """)
                .CloseError();
            }
            beforeExists = true;

            Expected("BEFORE");

            WriteBeforeAfter(beforeExists, afterExists);
        }

        if (CurrentEquals("AFTER"))
        {
            if (afterExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    After phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    AFTER can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to BEFORE.
                    """)
                .CloseError();
            }
            afterExists = true;

            Expected("AFTER");

            WriteBeforeAfter(beforeExists, afterExists);
        }
    }

    public static void SetLocale(SetLcValues locales = new())
    {
        if (CurrentEquals("LC_ALL"))
        {
            if (locales.LC_ALL)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132,"""
                    Locale phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    LC_ALL can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to each of the other locale names.
                    """)
                .CloseError();
            }
            locales.LC_ALL = true;

            Expected("LC_ALL");

            SetLocale(locales);
        }

        if (CurrentEquals("LC_COLLATE"))
        {
            if (locales.LC_COLLATE)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132,"""
                    Locale phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    LC_COLLATE can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to each of the other locale names.
                    """)
                .CloseError();
            }
            locales.LC_COLLATE = true;

            Expected("LC_COLLATE");

            SetLocale(locales);
        }

        if (CurrentEquals("LC_CTYPE"))
        {
            if (locales.LC_CTYPE)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132,"""
                    Locale phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    LC_CTYPE can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to each of the other locale names.
                    """)
                .CloseError();
            }
            locales.LC_CTYPE = true;

            Expected("LC_CTYPE");

            SetLocale(locales);
        }

        if (CurrentEquals("LC_MESSAGES"))
        {
            if (locales.LC_MESSAGES)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132,"""
                    Locale phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    LC_MESSAGES can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to each of the other locale names.
                    """)
                .CloseError();
            }
            locales.LC_MESSAGES = true;

            Expected("LC_MESSAGES");

            SetLocale(locales);
        }

        if (CurrentEquals("LC_MONETARY"))
        {
            if (locales.LC_MONETARY)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132,"""
                    Locale phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    LC_MONETARY can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to each of the other locale names.
                    """)
                .CloseError();
            }
            locales.LC_MONETARY = true;

            Expected("LC_MONETARY");

            SetLocale(locales);
        }

        if (CurrentEquals("LC_NUMERIC"))
        {
            if (locales.LC_NUMERIC)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132,"""
                    Locale phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    LC_NUMERIC can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to each of the other locale names.
                    """)
                .CloseError();
            }
            locales.LC_NUMERIC = true;

            Expected("LC_NUMERIC");

            SetLocale(locales);
        }

        if (CurrentEquals("LC_TIME"))
        {
            if (locales.LC_TIME)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132,"""
                    Locale phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    LC_TIME can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to each of the other locale names.
                    """)
                .CloseError();
            }
            locales.LC_TIME = true;

            Expected("LC_TIME");

            SetLocale(locales);
        }
    }

    public static void AtEndOfPage(ref bool isConditional, bool atEndOfPageExists = false, bool notAtEndOfPageExists = false)
    {
        if (CurrentEquals("AT END-OF-PAGE EOP"))
        {
            if (atEndOfPageExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    At end-of-page phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    AT END-OF-PAGE can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to NOT AT END-OF-PAGE.
                    """)
                .CloseError();
            }
            isConditional = true;
            atEndOfPageExists = true;

            Optional("AT");
            Choice("END-OF-PAGE EOP");

            Statements.WithoutSections(true);

            AtEndOfPage(ref isConditional, atEndOfPageExists, notAtEndOfPageExists);
        }

        if (CurrentEquals("NOT"))
        {
            if (notAtEndOfPageExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Not at end-of-page phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    NOT AT END-OF-PAGE can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to AT END-OF-PAGE.
                    """)
                .CloseError();
            }
            isConditional = true;
            notAtEndOfPageExists = true;

            Expected("NOT");
            Optional("AT");
            Choice("END-OF-PAGE EOP");

            Statements.WithoutSections(true);

            AtEndOfPage(ref isConditional, atEndOfPageExists, notAtEndOfPageExists);
        }
    }

    public static void ForAlphanumericForNational(bool forAlphanumericExists = false, bool forNationalExists = false)
    {
        if (CurrentEquals("FOR") && PeekEquals(1, "ALPHANUMERIC") || CurrentEquals("ALPHANUMERIC"))
        {
            if (forAlphanumericExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    For alphanumeric phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    FOR ALPHANUMERIC can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to FOR NATIONAL.
                    """)
                .CloseError();
            }
            forAlphanumericExists = true;

            Optional("FOR");
            Expected("ALPHANUMERIC");
            Optional("IS");

            References.Identifier();

            ForAlphanumericForNational(forAlphanumericExists, forNationalExists);
        }

        if (CurrentEquals("FOR") && PeekEquals(1, "NATIONAL") || CurrentEquals("NATIONAL"))
        {
            if (forNationalExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    For national phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    FOR NATIONAL can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to FOR ALPHANUMERIC.
                    """)
                .CloseError();
            }
            forNationalExists = true;

            Optional("FOR");
            Expected("NATIONAL");
            Optional("IS");

            References.Identifier();

            ForAlphanumericForNational(forAlphanumericExists, forNationalExists);
        }
    }

    public static void LineColumn(bool lineExists = false, bool columnExists = false)
    {
        if (CurrentEquals("LINE"))
        {
            if (lineExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Line number phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    LINE NUMBER can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to COLUMN NUMBER.
                    """)
                .CloseError();
            }
            lineExists = true;

            Expected("LINE");
            Optional("NUMBER");

            if (CurrentEquals(TokenType.Identifier))
            {
                References.Identifier();
            }
            else
            {
                Literals.Numeric();
            }

            LineColumn(lineExists, columnExists);
        }

        if (CurrentEquals("COLUMN COL"))
        {
            if (columnExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Column number phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    COLUMN NUMBER can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to LINE NUMBER.
                    """)
                .CloseError();
            }
            columnExists = true;

            Expected(Current().Value);
            Optional("NUMBER");

            if (CurrentEquals(TokenType.Identifier))
            {
                References.Identifier();
            }
            else
            {
                Literals.Numeric();
            }

            LineColumn(lineExists, columnExists);
        }
    }

    public static void RoundedPhrase()
    {
        Expected("ROUNDED");

        if (CurrentEquals("MODE"))
        {
            Expected("MODE");
            Optional("IS");

            Choice("AWAY-FROM-ZERO NEAREST-AWAY-FROM-ZERO NEAREST-EVEN NEAREST-TOWARD-ZERO PROHIBITED TOWARD-GREATER TOWARD-LESSER TRUNCATION");
        }
    }

    public static void Arithmetic(TokenContext delimiter)
    {
        var expression = new List<Token>();

        while (!CurrentEquals(TokenType.ReservedKeyword) && !CurrentEquals(delimiter) && !CurrentEquals("."))
        {
            BuildArithmeticExpression(expression);
        }

        if (!Expressions.IsBalanced(expression))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 320, """
                Unbalanced arithmetic expression.
                """)
            .WithSourceLine(expression[0], $"""
                one or more parenthesis don't have their matching pair.
                """)
            .CloseError();
        }

        var shuntingYard = Expressions.ShuntingYard(expression, Expressions.ArithmeticPrecedence);

        if (!Expressions.EvaluatePostfix(shuntingYard, Expressions.ArithmeticPrecedence, out Token error))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 325, """
                Invalid arithmetic expression.
                """)
            .WithSourceLine(expression[0], $"""
                This expression cannot be correctly evaluated.
                """)
            .WithNote("""
                Make sure that all operators have their matching operands.
                """)
            .CloseError();
        }
    }

    public static void Arithmetic(ReadOnlySpan<char> delimiters)
    {
        var expression = new List<Token>();

        while (!CurrentEquals(TokenType.ReservedKeyword) && !CurrentEquals(delimiters))
        {
            BuildArithmeticExpression(expression);
        }

        if (!Expressions.IsBalanced(expression))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 320, """
                Unbalanced arithmetic expression.
                """)
            .WithSourceLine(expression[0], $"""
                one or more parenthesis don't have their matching pair.
                """)
            .CloseError();
        }

        var shuntingYard = Expressions.ShuntingYard(expression, Expressions.ArithmeticPrecedence);

        if (!Expressions.EvaluatePostfix(shuntingYard, Expressions.ArithmeticPrecedence, out Token error))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 325, """
                Invalid arithmetic expression.
                """)
            .WithSourceLine(expression[0], $"""
                This expression cannot be correctly evaluated.
                """)
            .WithNote("""
                Make sure that all operators have their matching operands.
                """)
            .CloseError();
        }
    }

    public static void BuildArithmeticExpression(List<Token> expression)
    {
        static bool IsArithmeticSymbol(Token current)
        {
            return Expressions.ArithmeticPrecedence.ContainsKey(current.Value);
        }

        if (CurrentEquals(TokenType.Identifier | TokenType.Numeric))
        {
            expression.Add(Current());
            Continue();
        }

        if (IsArithmeticSymbol(Current()))
        {
            expression.Add(Current());
            Continue();
        }

        if (CurrentEquals(TokenType.Symbol) && !CurrentEquals(".") && !IsArithmeticSymbol(Current()))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 315, """
                Invalid arithmetic expression symbol.
                """)
            .WithSourceLine(Current(), $"""
                This symbol is invalid.
                """)
            .WithNote("""
                Valid operators are: +, -, *, /, **, ( and ).
                """)
            .CloseError();
        }
    }

    public static void Condition(TokenContext delimiter)
    {
        var expression = new List<Token>();

        while (!CurrentEquals(delimiter))
        {
            BuildConditionExpression(expression);
        }

        if (!Expressions.IsBalanced(expression))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 320, """
                Unbalanced conditional expression.
                """)
            .WithSourceLine(expression[0], $"""
                one or more parenthesis don't have their matching pair.
                """)
            .CloseError();
        }

        var shuntingYard = Expressions.ShuntingYard(expression, Expressions.ConditionalPrecedence);

        if (!Expressions.EvaluatePostfix(shuntingYard, Expressions.ConditionalPrecedence, out Token error))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 325, """
                Invalid arithmetic expression.
                """)
            .WithSourceLine(expression[0], $"""
                This expression cannot be correctly evaluated.
                """)
            .WithNote("""
                Make sure that all operators have their matching operands.
                """)
            .CloseError();
        }
    }

    public static void Condition(ReadOnlySpan<char> delimiters)
    {
        var expression = new List<Token>();

        while (!CurrentEquals(TokenContext.IsStatement) && !CurrentEquals(delimiters))
        {
            BuildConditionExpression(expression);
        }

        if (!Expressions.IsBalanced(expression))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 320, """
                Unbalanced conditional expression.
                """)
            .WithSourceLine(expression[0], $"""
                one or more parenthesis don't have their matching pair.
                """)
            .CloseError();
        }

        var shuntingYard = Expressions.ShuntingYard(expression, Expressions.ConditionalPrecedence);

        if (!Expressions.EvaluatePostfix(shuntingYard, Expressions.ConditionalPrecedence, out Token error))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 325, """
                Invalid arithmetic expression.
                """)
            .WithSourceLine(expression[0], $"""
                This expression cannot be correctly evaluated.
                """)
            .WithNote("""
                Make sure that all operators have their matching operands.
                """)
            .CloseError();
        }
    }

    public static void BuildConditionExpression(List<Token> expression)
    {
        if (CurrentEquals("IS") && (Peek(1).Value is "GREATER" or "LESS" or "EQUAL" or "NOT" || Peek(1).Type is TokenType.Symbol))
        {
            Continue();
        }
        else if (CurrentEquals("NOT") && (PeekEquals(1, ">") || PeekEquals(1, "<")))
        {
            var combined = new Token($"NOT {Peek(1).Value}", TokenType.Symbol, Current().Line, Current().Column);
            expression.Add(combined);
            Continue(2);
        }
        else if (CurrentEquals("NOT") && (PeekEquals(1, "GREATER") || PeekEquals(1, "LESS") || PeekEquals(1, "EQUAL")))
        {
            if (PeekEquals(1, "GREATER"))
            {
                var combined = new Token($"NOT >", TokenType.Symbol, Current().Line, Current().Column);
                expression.Add(combined);
            }

            if (PeekEquals(1, "LESS"))
            {
                var combined = new Token($"NOT <", TokenType.Symbol, Current().Line, Current().Column);
                expression.Add(combined);
            }

            if (PeekEquals(1, "EQUAL"))
            {
                var combined = new Token($"<>", TokenType.Symbol, Current().Line, Current().Column);
                expression.Add(combined);
            }

            Continue(2);

            if (CurrentEquals("THAN TO")) Continue();
        }
        else if (CurrentEquals("GREATER") || CurrentEquals("LESS") || CurrentEquals("EQUAL"))
        {
            if (CurrentEquals("GREATER"))
            {
                var converted = new Token($">", TokenType.Symbol, Current().Line, Current().Column);
                expression.Add(converted);
            }

            if (CurrentEquals("LESS"))
            {
                var converted = new Token($"<", TokenType.Symbol, Current().Line, Current().Column);
                expression.Add(converted);
            }

            if (CurrentEquals("EQUAL"))
            {
                var converted = new Token($"=", TokenType.Symbol, Current().Line, Current().Column);
                expression.Add(converted);
            }

            if (CurrentEquals("GREATER") && (PeekEquals(1, "OR") || PeekEquals(2, "OR")))
            {
                if (!PeekEquals(1, "THAN")) Continue(2);

                if (PeekEquals(1, "THAN")) Continue(3);

                var converted = new Token($">=", TokenType.Symbol, Current().Line, Current().Column);
                expression.Add(converted);
            }

            if (CurrentEquals("LESS") && (PeekEquals(1, "OR") || PeekEquals(2, "OR")))
            {
                if (PeekEquals(1, "THAN")) Continue(3);

                if (!PeekEquals(1, "THAN")) Continue(2);

                var converted = new Token($"<=", TokenType.Symbol, Current().Line, Current().Column);
                expression.Add(converted);
            }

            Continue();

            if (CurrentEquals("THAN TO")) Continue();
        }
        else
        {
            if (CurrentEquals("FUNCTION") || CurrentEquals(TokenType.Identifier) && PeekEquals(1, "("))
            {
                var current = Current();
                while (!CurrentEquals(")")) Continue();

                Continue();
                expression.Add(new Token("FUNCTION-CALL", TokenType.Identifier, current.Line, current.Column));
            }
            else
            {
                expression.Add(Current());
                Continue();
            }
        }
    }

    public static void StartRelationalOperator()
    {
        ReadOnlySpan<char> operators = "< > <= >= =";

        if (CurrentEquals("IS") && (PeekEquals(1, "GREATER LESS EQUAL NOT") || PeekEquals(1, TokenType.Symbol)))
        {
            Continue();
        }

        if (CurrentEquals("NOT") && PeekEquals(1, "> <"))
        {
            Continue(2);
        }
        else if (CurrentEquals("NOT") && PeekEquals(1, "GREATER LESS"))
        {
            Continue(2);

            if (CurrentEquals("THAN TO")) Continue();
        }
        else if (CurrentEquals("GREATER LESS EQUAL"))
        {
            if (CurrentEquals("GREATER") && (PeekEquals(1, "OR") || PeekEquals(2, "OR")))
            {
                if (!PeekEquals(1, "THAN")) Continue(2);

                if (PeekEquals(1, "THAN")) Continue(3);
            }

            if (CurrentEquals("LESS") && (PeekEquals(1, "OR") || PeekEquals(2, "OR")))
            {
                if (PeekEquals(1, "THAN")) Continue(3);

                if (!PeekEquals(1, "THAN")) Continue(2);

            }

            Continue();

            if (CurrentEquals("THAN TO")) Continue();
        }
        else if (CurrentEquals(operators))
        {
            Continue();
        }
        else
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 1, """
                Unexpected token type.
                """)
            .WithSourceLine(Current(), $"""
                Expected a relational operator.
                """)
            .WithNote("""
                With the exceptions being the "IS NOT EQUAL TO" and "IS NOT =" operators.
                """)
            .CloseError();

            Continue();
        }
    }

    public static void EncodingEndianness(bool encodingExists = false, bool endiannessExists = false)
    {
        if (CurrentEquals("BINARY-ENCODING DECIMAL-ENCODING"))
        {
            if (encodingExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Encoding phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    The encoding phrase can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to the endianness phrase.
                    """)
                .CloseError();
            }
            encodingExists = true;

            Expected(Current().Value);

            EncodingEndianness(encodingExists, endiannessExists);
        }

        if (CurrentEquals("HIGH-ORDER-LEFT HIGH-ORDER-RIGHT"))
        {
            if (endiannessExists)
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 132, """
                    Endianness phrase, duplicate definition.
                    """)
                .WithSourceLine(Current(), """
                    The endianness phrase can only be specified once in this statement.
                    """)
                .WithNote("""
                    The same applies to the encoding phrase.
                    """)
                .CloseError();
            }
            endiannessExists = true;

            Expected(Current().Value);

            EncodingEndianness(encodingExists, endiannessExists);
        }
    }

    public static EvaluateOperand SelectionSubject()
    {
        if (CurrentEquals(TokenType.Identifier | TokenType.Numeric | TokenType.String) && !PeekEquals(1, TokenType.Symbol))
        {
            if (CurrentEquals(TokenType.Identifier))
            {
                References.Identifier();
                return EvaluateOperand.Identifier;
            }

            ParseLiteral(true, true);
            return EvaluateOperand.Literal;
        }
        else if (CurrentEquals(TokenType.Identifier | TokenType.Numeric | TokenType.String) && PeekEquals(1, TokenType.Symbol))
        {
            if (Expressions.ArithmeticPrecedence.ContainsKey(Peek(1).Value))
            {
                Arithmetic("ALSO WHEN");
                return EvaluateOperand.Arithmetic;
            }
            else
            {
                Condition("ALSO WHEN");
                return EvaluateOperand.Condition;
            }
        }
        else if (CurrentEquals("TRUE FALSE"))
        {
            Choice("TRUE FALSE");
            return EvaluateOperand.TrueOrFalse;
        }

        return EvaluateOperand.Invalid;
    }

    public static void SelectionObject(EvaluateOperand operand)
    {
        bool identifier = operand is
            EvaluateOperand.Identifier or EvaluateOperand.Literal or
            EvaluateOperand.Arithmetic or EvaluateOperand.Boolean;

        bool literal = operand is
            EvaluateOperand.Identifier or EvaluateOperand.Arithmetic or
            EvaluateOperand.Boolean;

        bool arithmetic = operand is
            EvaluateOperand.Identifier or EvaluateOperand.Literal or
            EvaluateOperand.Arithmetic;

        bool boolean = operand is
            EvaluateOperand.Identifier or EvaluateOperand.Literal or
            EvaluateOperand.Boolean;

        bool range = operand is
            EvaluateOperand.Identifier or EvaluateOperand.Literal or
            EvaluateOperand.Arithmetic;

        bool condition = operand is
            EvaluateOperand.Condition or EvaluateOperand.TrueOrFalse;

        bool truefalse = operand is
            EvaluateOperand.Condition or EvaluateOperand.TrueOrFalse;

        if (identifier || literal && CurrentEquals(TokenType.Identifier | TokenType.Numeric | TokenType.String) && !PeekEquals(1, TokenType.Symbol))
        {
            if (identifier && CurrentEquals(TokenType.Identifier))
            {
                References.Identifier();
                RangeExpression(range, EvaluateOperand.Identifier);
            }
            else if (CurrentEquals("ANY"))
            {
                Expected("ANY");
            }
            else
            {
                ParseLiteral(true, true);
                RangeExpression(range, EvaluateOperand.Literal);
            }
        }
        else if (arithmetic || condition && CurrentEquals(TokenType.Identifier | TokenType.Numeric | TokenType.String) && PeekEquals(1, TokenType.Symbol))
        {
            if (arithmetic && Expressions.ArithmeticPrecedence.ContainsKey(Peek(1).Value))
            {
                Arithmetic("ALSO WHEN");
                RangeExpression(range, EvaluateOperand.Arithmetic);
            }
            else if (CurrentEquals("ANY"))
            {
                Expected("ANY");
            }
            else
            {
                Condition("ALSO WHEN");
            }
        }
        else if (truefalse && CurrentEquals("TRUE FALSE"))
        {
            Choice("TRUE FALSE ANY");
        }
        else if (CurrentEquals("ANY"))
        {
            Expected("ANY");
        }
    }

    public static void RangeExpression(bool canHaveRange, EvaluateOperand rangeType)
    {
        if (canHaveRange && CurrentEquals("THROUGH THRU"))
        {
            Choice("THROUGH THRU");
            if (rangeType is EvaluateOperand.Identifier)
            {
                References.Identifier();
            }
            else if (rangeType is EvaluateOperand.Literal)
            {
                ParseLiteral(true, true);
            }
            else if (rangeType is EvaluateOperand.Arithmetic)
            {
                Arithmetic("ALSO WHEN");
            }

            if (CurrentEquals("IS UTF-8"))
            {
                Optional("IS");
                // Need to implement other alphabet support
                Expected("UTF-8");
            }
        }
    }

    public static void ParseLiteral(bool numeric, bool @string)
    {
        if (!CurrentEquals(TokenType.Identifier | TokenType.Numeric | TokenType.String))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 1, """
                Unexpected token type.
                """)
            .WithSourceLine(Current(), $"""
                Expected an identifier or a literal.
                """)
            .CloseError();
        }

        if (numeric && CurrentEquals(TokenType.Numeric))
        {
            Literals.Numeric();
        }
        else if (@string && CurrentEquals(TokenType.String))
        {
            Literals.String();
        }
    }

    public static bool NotIdentifierOrLiteral()
    {
        return !IdentifierOrLiteral();
    }

    public static bool IdentifierOrLiteral()
    {
        return CurrentEquals(TokenType.Identifier | TokenType.Numeric | TokenType.String | TokenType.HexString | TokenType.Boolean | TokenType.HexBoolean | TokenType.National | TokenType.HexNational | TokenType.Figurative);
    }

    public static bool IdentifierOrLiteral(TokenType literalType)
    {
        return CurrentEquals(TokenType.Identifier | literalType);
    }
}