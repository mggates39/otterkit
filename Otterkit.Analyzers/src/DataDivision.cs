using static Otterkit.Types.TokenHandling;
using Otterkit.Types;

namespace Otterkit.Analyzers;

public static partial class DataDivision
{
    /// <summary>
    /// Stack int <c>LevelStack</c> is used in the parser whenever it needs to know which data item level it is currently parsing.
    /// <para>This is used when handling the level number syntax rules, like which clauses are allowed for a particular level number or group item level number rules</para>
    /// </summary>
    private static readonly Stack<int> LevelStack = new();

    /// <summary>
    /// Stack string <c>GroupStack</c> is used in the parser whenever it needs to know which group the current data item belongs to.
    /// <para>This is used when handling the group item syntax rules, like which data items belong to which groups</para>
    /// </summary>
    private static readonly Stack<DataEntry> GroupStack = new();

    // Method responsible for parsing the DATA DIVISION.
    // That includes the FILE, WORKING-STORAGE, LOCAL-STORAGE, LINKAGE, REPORT and SCREEN sections.
    // It is also responsible for showing appropriate error messages when an error occurs in the DATA DIVISION.
    public static void Parse()
    {
        Expected("DATA");
        Expected("DIVISION");
        CompilerContext.ActiveScope = SourceScope.DataDivision;

        if (!Expected(".", false))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 25,"""
                Division header, missing separator period.
                """)
            .WithSourceLine(Peek(-1), """
                Expected a separator period '. ' after this token
                """)
            .WithNote("""
                Every division header must end with a separator period
                """)
            .CloseError();

            AnchorPoint("WORKING-STORAGE LOCAL-STORAGE LINKAGE PROCEDURE");
        }

        if (CurrentEquals("FILE"))
            FileSection();

        if (CurrentEquals("WORKING-STORAGE"))
            WorkingStorage();

        if (CurrentEquals("LOCAL-STORAGE"))
            LocalStorage();

        if (CurrentEquals("LINKAGE"))
            LinkageSection();

        if (CurrentEquals("REPORT"))
            ReportSection();

        if (CurrentEquals("SCREEN"))
            ScreenSection();
    }

    // The following methods are responsible for parsing the DATA DIVISION sections
    // They are technically only responsible for parsing the section header, 
    // the Entries() method handles parsing the actual data items in their correct sections.
    private static void FileSection()
    {
        Expected("FILE");
        Expected("SECTION");
        CompilerContext.ActiveScope = SourceScope.FileSection;

        Expected(".");
        while (CurrentEquals("FD SD"))
        {
            FileEntry();
        }
    }

    private static void ReportSection()
    {
        Expected("REPORT");
        Expected("SECTION");
        CompilerContext.ActiveScope = SourceScope.ReportSection;

        Expected(".");
        while (CurrentEquals("RD"))
        {
            ReportEntry();
        }
    }

    private static void ScreenSection()
    {
        Expected("SCREEN");
        Expected("SECTION");
        CompilerContext.ActiveScope = SourceScope.ScreenSection;

        Expected(".");
        while (CurrentEquals(TokenType.Numeric))
        {
            ScreenEntries();
        }
    }

    private static void WorkingStorage()
    {
        Expected("WORKING-STORAGE");
        Expected("SECTION");
        CompilerContext.ActiveScope = SourceScope.WorkingStorage;

        Expected(".");
        while (CurrentEquals(TokenType.Numeric))
        {
            DataEntries();
        }
    }

    private static void LocalStorage()
    {
        Expected("LOCAL-STORAGE");
        Expected("SECTION");
        CompilerContext.ActiveScope = SourceScope.LocalStorage;

        Expected(".");
        while (Current().Type is TokenType.Numeric)
        {
            DataEntries();
        }
    }

    private static void LinkageSection()
    {
        Expected("LINKAGE");
        Expected("SECTION");
        CompilerContext.ActiveScope = SourceScope.LinkageSection;

        Expected(".");
        while (Current().Type is TokenType.Numeric)
        {
            DataEntries();
        }
    }


    // The following methods are responsible for parsing the DATA DIVISION data items
    // The Entries() method is responsible for identifying which kind of data item to 
    // parse based on it's level number.

    // The GroupEntry(), DataEntry(), and ConstantEntry() are then responsible for correctly
    // parsing each data item, or in the case of the GroupEntry() a group item or 01-level elementary item.
    private static void DataEntries()
    {
        if (CurrentEquals("77"))
            DataEntry();

        if ((CurrentEquals("01") || CurrentEquals("1")) && !PeekEquals(2, "CONSTANT"))
            GroupEntry();

        if (PeekEquals(2, "CONSTANT"))
            ConstantEntry();
    }

    private static void ReportEntries()
    {
        if (!CurrentEquals("01 1"))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 2,"""
                Unexpected token.
                """)
            .WithSourceLine(Current(), """
                Expected a 01 level number.
                """)
            .WithNote("""
                Root level records must have a 01 level number.
                """)
            .CloseError(); 
        }

        if (CurrentEquals("01 1") && !PeekEquals(2, "CONSTANT"))
            GroupEntry();

        if (PeekEquals(2, "CONSTANT"))
            ConstantEntry();
    }

    private static void RecordGroupEntries()
    {
        if (!CurrentEquals("01 1"))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 2,"""
                Unexpected token.
                """)
            .WithSourceLine(Current(), """
                Expected a 01 level number.
                """)
            .WithNote("""
                Root level records must have a 01 level number.
                """)
            .CloseError(); 
        }

        if (CurrentEquals("01 1") && !PeekEquals(2, "CONSTANT"))
            GroupEntry();

        if (PeekEquals(2, "CONSTANT"))
            ConstantEntry();
    }

    private static void ScreenEntries()
    {
        if (!CurrentEquals("01 1"))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 2,"""
                Unexpected token.
                """)
            .WithSourceLine(Current(), """
                Expected a 01 level number.
                """)
            .WithNote("""
                Root level screen items must have a 01 level number.
                """)
            .CloseError(); 
        }

        if (CurrentEquals("01 1") && !PeekEquals(2, "CONSTANT"))
            GroupEntry();

        if (PeekEquals(2, "CONSTANT"))
            ConstantEntry();
    }

    private static void ReportEntry()
    {
        Expected("RD");

        References.Identifier();

        if (!CurrentEquals(TokenContext.IsClause) && !CurrentEquals("."))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 2,"""
                Unexpected token.
                """)
            .WithSourceLine(Peek(-1), """
                Expected report description clauses or a separator period after this token.
                """)
            .CloseError();
        }

        while (CurrentEquals(TokenContext.IsClause))
        {
            ReportEntryClauses();
        }

        if (!Expected(".", false))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 25,"""
                Report description, missing separator period.
                """)
            .WithSourceLine(Peek(-1), """
                Expected a separator period '. ' after this token.
                """)
            .WithNote("""
                Every RD item must end with a separator period.
                """)
            .CloseError();
        }

        while(CurrentEquals(TokenType.Numeric))
        {
            ReportEntries();
        }
    }

    private static void ReportGroupEntry()
    {
        int levelNumber = int.Parse(Current().Value);
        Literals.Numeric();

        Token itemToken = Current();
        string dataName = itemToken.Value;

        CheckLevelNumber(levelNumber);

        if (CurrentEquals("FILLER"))
        {
            Expected("FILLER");
        }
        else if (CurrentEquals(TokenType.Identifier))
        {
            References.Identifier();
        }

        DataEntry dataLocal = new(itemToken, EntryKind.ReportGroupDescription);

        dataLocal.LevelNumber = levelNumber;
        dataLocal.Section = CompilerContext.ActiveScope;

        if (GroupStack.Count is not 0)
        {
            dataLocal.Parent = GroupStack.Peek();
        }

        if (!CurrentEquals(TokenContext.IsClause) && !CurrentEquals("."))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 2,"""
                Unexpected token.
                """)
            .WithSourceLine(Peek(-1), """
                Expected report item clauses or a separator period after this token
                """)
            .CloseError();
        }

        while (CurrentEquals(TokenContext.IsClause))
        {
            ReportGroupClauses(dataLocal);
        }

        HandleLevelStack(dataLocal);

        if (dataLocal.IsGroup) GroupStack.Push(dataLocal);

        if (!Expected(".", false))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 25,"""
                Report item definition, missing separator period.
                """)
            .WithSourceLine(Peek(-1), """
                Expected a separator period '. ' after this token
                """)
            .WithNote("""
                Every item must end with a separator period
                """)
            .CloseError();
        }

        CheckConditionNames(dataLocal);

        // We're returning during a resolution pass
        if (CompilerContext.IsResolutionPass) return;

        if (dataName is "FILLER") return;

        // Because we don't want to run this again during it
        var sourceUnit = CompilerContext.ActiveCallable;

        if (sourceUnit.DataNames.Exists(itemToken) && levelNumber is 1 or 77)
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 30,"""
                Duplicate root level definition.
                """)
            .WithSourceLine(itemToken, """
                A 01 or 77 level variable already exists with this name.
                """)
            .WithNote("""
                Every root level item must have a unique name. 
                """)
            .CloseError();
        }

        sourceUnit.DataNames.Add(itemToken, dataLocal);
    }

    private static void FileEntry()
    {
        Choice("FD SD");

        Token itemToken = Current();
        string fileName = itemToken.Value;

        References.Identifier();

        DataEntry fileLocal = new(itemToken, EntryKind.FileDescription);

        fileLocal.Section = CompilerContext.ActiveScope;

        if (!CurrentEquals(TokenContext.IsClause) && !CurrentEquals("."))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 2,"""
                Unexpected token.
                """)
            .WithSourceLine(Peek(-1), """
                Expected file description clauses or a separator period after this token
                """)
            .CloseError();
        }

        while (CurrentEquals(TokenContext.IsClause))
        {
            FileEntryClauses(fileLocal);
        }

        if (!Expected(".", false))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 25,"""
                File description, missing separator period.
                """)
            .WithSourceLine(Peek(-1), """
                Expected a separator period '. ' after this token
                """)
            .WithNote("""
                Every FD item must end with a separator period
                """)
            .CloseError();
        }

        while(CurrentEquals(TokenType.Numeric))
        {
            RecordGroupEntries();
        }

        // We're returning during a resolution pass
        if (CompilerContext.IsResolutionPass) return;

        // Because we don't want to run this again during it
        var sourceUnit = CompilerContext.ActiveCallable;

        if (sourceUnit.DataNames.Exists(itemToken))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 30,"""
                Duplicate root level definition.
                """)
            .WithSourceLine(itemToken, """
                A root level variable already exists with this name.
                """)
            .WithNote("""
                Every root level item must have a unique name. 
                """)
            .CloseError();
        }

        sourceUnit.DataNames.Add(itemToken, fileLocal);
    }

    private static void GroupEntryKind()
    {
        if (CompilerContext.ActiveScope is SourceScope.ScreenSection)
        {
            ScreenEntry();
            return;
        }

        if (CompilerContext.ActiveScope is SourceScope.ReportSection)
        {
            ReportGroupEntry();
            return;
        }

        DataEntry();
    }

    private static void GroupEntry()
    {
        GroupEntryKind();

        _ = int.TryParse(Current().Value, out int level);
        
        while (level > 1 && level < 50)
        {
            GroupEntryKind();

            _ = int.TryParse(Current().Value, out level);
        }

        if (CompilerContext.IsResolutionPass)
        {
            CalculateGroupLengths();
        }

        LevelStack.Clear();
        GroupStack.Clear();
    }

    private static void CalculateGroupLengths()
    {
        foreach (var entry in GroupStack)
        {
            if (entry.Parent.Exists)
            {
                entry.Parent.Unwrap().Length += entry.Length;
            }
        }
    }

    private static void DataEntry()
    {
        var levelNumber = int.Parse(Current().Value);

        Literals.Numeric();

        CheckLevelNumber(levelNumber);

        Token variable;

        if (CurrentEquals(TokenType.Identifier))
        {
            variable = References.LocalDefinition().Unwrap();
        }
        else if (CurrentEquals("FILLER"))
        {
            variable = Current();

            Optional("FILLER");
        }
        else
        {
            variable = new("[FILLER]", TokenType.ReservedKeyword)
            {
                Line = CurrentLine(),
                Column = CurrentColumn(),
                FileIndex = CurrentFile()
            };
        }

        var entry = new DataEntry(variable, EntryKind.DataDescription);

        entry.LevelNumber = levelNumber;

        entry.Section = CompilerContext.ActiveScope;

        if (!CurrentEquals(TokenContext.IsClause) && !CurrentEquals("."))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 2,"""
                Unexpected token.
                """)
            .WithSourceLine(Peek(-1), """
                Expected data item clauses or a separator period after this token
                """)
            .CloseError();
        }

        entry.DeclarationIndex = TokenHandling.Index;

        // COBOL standard requirement
        // Usage display is the default unless specified otherwise
        entry.Usage = Usages.Display;

        while (CurrentEquals(TokenContext.IsClause))
        {
            DataEntryClauses(entry);
        }

        if (!Expected(".", false))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 25,"""
                Data item definition, missing separator period.
                """)
            .WithSourceLine(Peek(-1), """
                Expected a separator period '. ' after this token
                """)
            .WithNote("""
                Every item must end with a separator period
                """)
            .CloseError();
        }

        HandleLevelStack(entry);

        if (entry.IsGroup) GroupStack.Push(entry);

        CheckClauseCompatibility(entry, variable);

        CheckConditionNames(entry);

        if (GroupStack.Count is not 0)
        {
            var parent = GroupStack.Peek();

            entry.Parent = parent;

            parent.Length += entry.Length;
        }

        // We're returning during a resolution pass
        if (CompilerContext.IsResolutionPass) return;

        // We're also returning for filler items, they shouldn't be added to the symbol table
        if (variable.Value is "FILLER" or "[FILLER]") return;

        // Because we don't want to run this again during it
        var sourceUnit = CompilerContext.ActiveCallable;

        if (sourceUnit.DataNames.Exists(variable) && levelNumber is 1 or 77)
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 30,"""
                Duplicate root level definition.
                """)
            .WithSourceLine(variable, """
                A 01 or 77 level variable already exists with this name.
                """)
            .WithNote("""
                Every root level item must have a unique name. 
                """)
            .CloseError();
        }

        sourceUnit.DataNames.Add(variable, entry);
    }

    private static void ConstantEntry()
    {
        if (!CurrentEquals("01") && !CurrentEquals("1"))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 40,"""
                Invalid level number.
                """)
            .WithSourceLine(Current(), """
                CONSTANT variables must have a level number of '1' or '01'.
                """)
            .CloseError();
        }

        var levelNumber = int.Parse(Current().Value);
        Literals.Numeric();

        Token itemToken = Current();
        string dataName = itemToken.Value;

        References.Identifier();

        DataEntry dataLocal = new(itemToken, EntryKind.DataDescription);

        dataLocal.LevelNumber = levelNumber;
        dataLocal.Section = CompilerContext.ActiveScope;
        dataLocal.IsConstant = true;

        Expected("CONSTANT");
        if (CurrentEquals("IS") || CurrentEquals("GLOBAL"))
        {
            Optional("IS");
            Expected("GLOBAL");
            dataLocal[DataClause.Global] = true;
        }

        if (CurrentEquals("FROM"))
        {
            Expected("FROM");
            References.Identifier();
        }
        else
        {
            Optional("AS");
            switch (Current().Type)
            {
                case TokenType.String:
                    Literals.String();
                    break;

                case TokenType.Numeric:
                    Literals.Numeric();
                    break;

                case TokenType.Figurative:
                    Literals.Figurative();
                    break;
            }

            if (CurrentEquals("LENGTH"))
            {
                Expected("LENGTH");
                Optional("OF");
                References.Identifier();
            }

            if (CurrentEquals("BYTE-LENGTH"))
            {
                Expected("BYTE-LENGTH");
                Optional("OF");
                References.Identifier();
            }

        }

        if (!Expected(".", false))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 25,"""
                Data item definition, missing separator period.
                """)
            .WithSourceLine(Peek(-1), """
                Expected a separator period '. ' after this token
                """)
            .WithNote("""
                Every item must end with a separator period
                """)
            .CloseError();
        }

        // We're returning during a resolution pass
        if (CompilerContext.IsResolutionPass) return;

        // Because we don't want to run this again during it
        var sourceUnit = CompilerContext.ActiveCallable;

        if (sourceUnit.DataNames.Exists(itemToken) && levelNumber is 1 or 77)
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 30,"""
                Duplicate root level definition.
                """)
            .WithSourceLine(itemToken, """
                A 01 or 77 level variable already exists with this name.
                """)
            .WithNote("""
                Every root level item must have a unique name. 
                """)
            .CloseError();
        }

        sourceUnit.DataNames.Add(itemToken, dataLocal);
    }

    private static void ScreenEntry()
    {
        var levelNumber = int.Parse(Current().Value);

        Literals.Numeric();

        CheckLevelNumber(levelNumber);

        Token variable;

        if (CurrentEquals(TokenType.Identifier))
        {
            variable = References.LocalDefinition().Unwrap();
        }
        else if (CurrentEquals("FILLER"))
        {
            variable = Current();

            Optional("FILLER");
        }
        else
        {
            variable = new("[FILLER]", TokenType.ReservedKeyword)
            {
                Line = CurrentLine(),
                Column = CurrentColumn(),
                FileIndex = CurrentFile()
            };
        }

        var entry = new DataEntry(variable, EntryKind.ScreenDescription);

        entry.LevelNumber = levelNumber;

        entry.Section = CompilerContext.ActiveScope;

        if (!CurrentEquals(TokenContext.IsClause) && !CurrentEquals("."))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 2,"""
                Unexpected token.
                """)
            .WithSourceLine(Peek(-1), """
                Expected screen item clauses or a separator period after this token
                """)
            .CloseError();
        }

        entry.DeclarationIndex = TokenHandling.Index;

        // COBOL standard requirement
        // Usage display is the default unless specified otherwise
        entry.Usage = Usages.Display;

        while (CurrentEquals(TokenContext.IsClause))
        {
            ScreenEntryClauses(entry);
        }

        if (!Expected(".", false))
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 25,"""
                Screen item definition, missing separator period.
                """)
            .WithSourceLine(Peek(-1), """
                Expected a separator period '. ' after this token
                """)
            .WithNote("""
                Every item must end with a separator period
                """)
            .CloseError();
        }

        HandleLevelStack(entry);

        if (entry.IsGroup) GroupStack.Push(entry);

        CheckClauseCompatibility(entry, variable);

        CheckConditionNames(entry);

        if (GroupStack.Count is not 0)
        {
            var parent = GroupStack.Peek();

            entry.Parent = parent;

            parent.Length += entry.Length;
        }

        // We're returning during a resolution pass
        if (CompilerContext.IsResolutionPass) return;

        // We're also returning for filler items, they shouldn't be added to the symbol table
        if (variable.Value is "FILLER" or "[FILLER]") return;

        // Because we don't want to run this again during it
        var sourceUnit = CompilerContext.ActiveCallable;

        if (sourceUnit.DataNames.Exists(variable) && levelNumber is 1 or 77)
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 30,"""
                Duplicate root level definition.
                """)
            .WithSourceLine(variable, """
                A 01 or 77 level variable already exists with this name.
                """)
            .WithNote("""
                Every root level item must have a unique name. 
                """)
            .CloseError();
        }

        sourceUnit.DataNames.Add(variable, entry);
    }

    private static void HandleLevelStack(DataEntry entryLocal)
    {
        if (CurrentEquals(TokenType.Numeric) && LevelStack.Count > 0)
        {
            _ = int.TryParse(Current().Value, out int level);
            var currentLevel = LevelStack.Peek();

            if (currentLevel == 1 && level >= 2 && level <= 49 || level >= 2 && level <= 49 && level > currentLevel)
            {
                entryLocal.IsGroup = true;
            }
        }
    }

    private static void CheckLevelNumber(int level)
    {
        if (level is 66 or 77 or 88) return;

        if (level is 1)
        {
            LevelStack.Push(level);
            return;
        }

        var currentLevel = LevelStack.Peek();

        if (level == currentLevel) return;

        if (level > currentLevel && level <= 49)
        {
            LevelStack.Push(level);
            return;
        }

        if (level < currentLevel)
        {
            var current = LevelStack.Pop();
            var lowerLevel = LevelStack.Peek();
            if (level == lowerLevel)
            {
                GroupStack.Pop();
                return;
            }

            if (level != lowerLevel)
            {
                LevelStack.Push(current);

                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 40,"""
                    Invalid level number.
                    """)
                .WithSourceLine(Current(), $"""
                    This variable should have a level number of {lowerLevel}.
                    """)
                .CloseError();
            }
        }
    }

    private static void CheckClauseCompatibility(DataEntry dataItem, Token itemToken)
    {
        bool usageCannotHavePicture = dataItem.Usage switch
        {
            Usages.BinaryChar => true,
            Usages.BinaryDouble => true,
            Usages.BinaryLong => true,
            Usages.BinaryShort => true,
            Usages.FloatShort => true,
            Usages.FloatLong => true,
            Usages.FloatExtended => true,
            Usages.Index => true,
            Usages.MessageTag => true,
            Usages.ObjectReference => true,
            Usages.DataPointer => true,
            Usages.FunctionPointer => true,
            Usages.ProgramPointer => true,
            _ => false
        };

        if (usageCannotHavePicture && dataItem[DataClause.Picture])
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 45,"""
                Invalid clause combination.
                """)
            .WithSourceLine(itemToken, $"""
                Items with USAGE {dataItem.Usage.Display()} must not contain a PICTURE clause.
                """)
            .CloseError();
        }

        if (!usageCannotHavePicture && !dataItem.IsGroup && !dataItem[DataClause.Type] && !dataItem[DataClause.Picture] && !dataItem[DataClause.Usage] && !dataItem[DataClause.Value])
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 45,"""
                Invalid clause combination.
                """)
            .WithSourceLine(itemToken, $"""
                Elementary items must contain a PICTURE clause.
                """)
            .CloseError();
        }

        if (dataItem.IsGroup && dataItem[DataClause.Picture])
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 45,"""
                Invalid clause combination.
                """)
            .WithSourceLine(itemToken, $"""
                Group items must not contain a PICTURE clause.
                """)
            .CloseError();
        }

        if (dataItem[DataClause.Renames] && dataItem[DataClause.Picture])
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 45,"""
                Invalid clause combination.
                """)
            .WithSourceLine(itemToken, $"""
                Items with a RENAMES clause must not contain a PICTURE clause.
                """)
            .CloseError();
        }

        bool usageCannotHaveValue = dataItem.Usage switch
        {
            Usages.Index => true,
            Usages.MessageTag => true,
            Usages.ObjectReference => true,
            Usages.DataPointer => true,
            Usages.FunctionPointer => true,
            Usages.ProgramPointer => true,
            _ => false
        };

        if (usageCannotHaveValue && dataItem[DataClause.Value])
        {
            ErrorHandler
            .Build(ErrorType.Analyzer, ConsoleColor.Red, 45,"""
                Invalid clause combination.
                """)
            .WithSourceLine(itemToken, $"""
                Items with USAGE {dataItem.Usage.Display()} must not contain a VALUE clause.
                """)
            .CloseError();
        }
    }

    private static void CheckConditionNames(DataEntry parent)
    {
        if (!CurrentEquals("88")) return;

        while (CurrentEquals("88"))
        {
            Expected("88");

            Token itemToken = Current();
            string dataName = itemToken.Value;

            References.Identifier();

            DataEntry dataLocal = new(itemToken, EntryKind.DataDescription);

            dataLocal.Parent = parent;
            dataLocal.LevelNumber = 88;
            dataLocal.Section = CompilerContext.ActiveScope;

            if (CurrentEquals("VALUES"))
            {
                Expected("VALUES");
                Optional("ARE");
            }
            else
            {
                Expected("VALUE");
                Optional("IS");
            }

            var firstConditionType = Current().Type;

            switch (Current().Type)
            {
                case TokenType.Numeric: Literals.Numeric(); break;
                
                case TokenType.String:
                case TokenType.HexString:
                case TokenType.Boolean:
                case TokenType.HexBoolean:
                case TokenType.National:
                case TokenType.HexNational:
                    Literals.String(); break;
            }

            if (CurrentEquals("THROUGH THRU"))
            {
                Choice("THROUGH THRU");

                switch (firstConditionType)
                {
                    case TokenType.Numeric: Literals.Numeric(); break;
                    
                    case TokenType.String:
                    case TokenType.HexString:
                    case TokenType.Boolean:
                    case TokenType.HexBoolean:
                    case TokenType.National:
                    case TokenType.HexNational:
                        Literals.String(); break;
                }
            }

            if (!Expected(".", false))
            {
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 25,"""
                    Data item definition, missing separator period.
                    """)
                .WithSourceLine(Peek(-1), """
                    Expected a separator period '. ' after this token
                    """)
                .WithNote("""
                    Every item must end with a separator period
                    """)
                .CloseError();
            }

            // We're returning during a resolution pass
            if (CompilerContext.IsResolutionPass) continue;

            // Because we don't want to run this again during it
            var sourceUnit = CompilerContext.ActiveCallable;

            if (sourceUnit.DataNames.Exists(itemToken))
            {
                // TODO: This is incorrect, but was done to replace the old error message system
                ErrorHandler
                .Build(ErrorType.Analyzer, ConsoleColor.Red, 30,"""
                    Duplicate condition name definition.
                    """)
                .WithSourceLine(itemToken, """
                    A condition variable already exists with this name
                    """)
                .WithNote("""
                    condition items must have a unique name. 
                    """)
                .CloseError();
            }

            sourceUnit.DataNames.Add(itemToken, dataLocal);
        }
    }
}
