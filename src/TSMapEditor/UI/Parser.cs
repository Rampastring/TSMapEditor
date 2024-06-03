using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Text;

namespace TSMapEditor.UI
{
    /// <summary>
    /// Parses arithmetic expressions.
    /// </summary>
    class Parser
    {
        private const int CHAR_VALUE_ZERO = 48;

        public Parser(WindowManager windowManager)
        {
            if (_instance != null)
                throw new InvalidOperationException("Only one instance of Parser can exist at a time.");

            globalConstants = new Dictionary<string, int>();
            RefreshResolutionConstants(windowManager);
            globalConstants.Add("EMPTY_SPACE_TOP", Constants.UIEmptyTopSpace);
            globalConstants.Add("EMPTY_SPACE_BOTTOM", Constants.UIEmptyBottomSpace);
            globalConstants.Add("EMPTY_SPACE_SIDES", Constants.UIEmptySideSpace);
            globalConstants.Add("HORIZONTAL_SPACING", Constants.UIHorizontalSpacing);
            globalConstants.Add("VERTICAL_SPACING", Constants.UIVerticalSpacing);
            globalConstants.Add("BUTTON_HEIGHT", Constants.UIButtonHeight);
            globalConstants.Add("IS_RA2YR", Constants.IsRA2YR ? 1 : 0);
            globalConstants.Add("OBJECT_HEALTH_MAX", Constants.ObjectHealthMax);
            globalConstants.Add("IS_FLAT_WORLD", Constants.IsFlatWorld ? 1 : 0);

            _instance = this;
        }

        public void RefreshResolutionConstants(WindowManager windowManager)
        {
            globalConstants["RESOLUTION_WIDTH"] = windowManager.RenderResolutionX;
            globalConstants["RESOLUTION_HEIGHT"] = windowManager.RenderResolutionY;
        }

        private static Parser _instance;
        public static Parser Instance => _instance;

        private static Dictionary<string, int> globalConstants;

        public string Input { get; private set; }

        private int tokenPlace;
        private XNAControl primaryControl;
        private XNAControl parsingControl;

        private XNAControl GetControl(string controlName)
        {
            if (controlName == primaryControl.Name)
                return primaryControl;

            var control = Find(primaryControl.Children, controlName);
            if (control == null)
                throw new KeyNotFoundException($"Control '{controlName}' not found while parsing input '{Input}'");

            return control;
        }

        private XNAControl Find(IEnumerable<XNAControl> list, string controlName)
        {
            foreach (XNAControl child in list)
            {
                if (child.Name == controlName)
                    return child;

                XNAControl childOfChild = Find(child.Children, controlName);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        private int GetConstant(string constantName)
        {
            return globalConstants[constantName];
        }

        public void SetPrimaryControl(XNAControl primaryControl)
        {
            this.primaryControl = primaryControl;
        }

        public int GetExprValue(string input, XNAControl parsingControl)
        {
            this.parsingControl = parsingControl;
            Input = input;
            tokenPlace = 0;
            return GetExprValue();
        }

        /// <summary>
        /// Parsing a sub-expression while parsing an expression eradicates the original parsing context
        /// (information about the input and token place).
        ///
        /// This function automatically saves and restores the original context while parsing a sub-expression.
        /// </summary>
        private int GetExprValueWithContextSave(string input, XNAControl parsingControl)
        {
            string originalInput = Input;
            int originalTokenPlace = tokenPlace;
            int value = GetExprValue(input, parsingControl);
            Input = originalInput;
            tokenPlace = originalTokenPlace;

            return value;
        }

        private int GetExprValue()
        {
            int value = 0;

            while (true)
            {
                SkipWhitespace();

                if (IsEndOfInput())
                    return value;

                char c = PeekChar();

                if (char.IsDigit(c))
                {
                    value = GetInt();
                }
                else if (c == '+')
                {
                    tokenPlace++;
                    value += GetNumericalValue();
                }
                else if (c == '-')
                {
                    tokenPlace++;
                    value -= GetNumericalValue();
                }
                else if (c == '/')
                {
                    tokenPlace++;
                    value /= GetExprValue();
                }
                else if (c == '*')
                {
                    tokenPlace++;
                    value *= GetExprValue();
                }
                else if (c == '(')
                {
                    tokenPlace++;
                    value = GetExprValue();
                }
                else if (c == ')')
                {
                    tokenPlace++;
                    return value;
                }
                else if (char.IsUpper(c))
                {
                    value = GetConstantValue();
                }
                else if (char.IsLower(c))
                {
                    value = GetFunctionValue();
                }
            }
        }

        private int GetNumericalValue()
        {
            SkipWhitespace();

            if (IsEndOfInput())
                return 0;

            char c = PeekChar();

            if (char.IsDigit(c))
            {
                return GetInt();
            }
            else if (char.IsUpper(c))
            {
                return GetConstantValue();
            }
            else if (char.IsLower(c))
            {
                return GetFunctionValue();
            }
            else if (c == '(')
            {
                tokenPlace++;
                return GetExprValue();
            }
            else
                throw new INIConfigException("Unexpected character " + c + " when parsing input: " + Input);
        }

        private void SkipWhitespace()
        {
            while (true)
            {
                if (IsEndOfInput())
                    return;

                char c = PeekChar();
                if (c == ' ' || c == '\r' || c == '\n')
                    tokenPlace++;
                else
                    break;
            }
        }

        private string GetIdentifier()
        {
            string identifierName = "";

            while (true)
            {
                if (IsEndOfInput())
                    break;

                char c = PeekChar();
                if (char.IsWhiteSpace(c))
                    break;

                if (!char.IsLetterOrDigit(c) && c != '_')
                    break;

                identifierName += c.ToString();
                tokenPlace++;
            }

            return identifierName;
        }

        private int GetConstantValue()
        {
            string constantName = GetIdentifier();
            return GetConstant(constantName);
        }

        private int GetFunctionValue()
        {
            string functionName = GetIdentifier();
            SkipWhitespace();
            ConsumeChar('(');

            var parameters = new List<string>();

            var sb = new StringBuilder();

            // Read all parameters
            while (true)
            {
                SkipWhitespace();

                sb.Clear();
                int openParenCount = 0;

                // Fetch single parameter, read until ',' or ')'
                while (true)
                {
                    char c = PeekChar();

                    if (c == ',')
                    {
                        break;
                    }

                    if (c == '(')
                    {
                        openParenCount++;
                    }
                    else if (c == ')')
                    {
                        openParenCount--;
                        if (openParenCount < 0)
                            break;
                    }

                    sb.Append(c);
                    ConsumeChar(c);
                }

                string paramName = sb.ToString();
                parameters.Add(paramName);

                if (PeekChar() != ',')
                    break;

                ConsumeChar(',');
            }

            SkipWhitespace();
            ConsumeChar(')');

            // Evaluate function
            switch (functionName)
            {
                case "getX":
                    return GetControl(parameters[0]).X;
                case "getY":
                    return GetControl(parameters[0]).Y;
                case "getWidth":
                    return GetControl(parameters[0]).Width;
                case "getHeight":
                    return GetControl(parameters[0]).Height;
                case "getBottom":
                    return GetControl(parameters[0]).Bottom;
                case "getRight":
                    return GetControl(parameters[0]).Right;
                case "max":
                    int largest = GetExprValueWithContextSave(parameters[0], parsingControl);
                    for (int i = 1; i < parameters.Count; i++)
                    {
                        int val = GetExprValueWithContextSave(parameters[i], parsingControl);
                        if (val > largest)
                            largest = val;
                    }
                    return largest;
                case "horizontalCenterOnParent":
                    parsingControl.CenterOnParentHorizontally();
                    return parsingControl.X;
                default:
                    throw new INIConfigException("Unknown function " + functionName + " in expression " + Input);
            }
        }

        private void ConsumeChar(char token)
        {
            if (Input[tokenPlace] != token)
                throw new INIConfigException("Parse error: expected '" + token + "' in expression " + Input);
            tokenPlace++;
        }

        private char PeekChar()
        {
            if (IsEndOfInput())
                throw new INIConfigException("Parse error: unexpected end of input in expression " + Input);

            return Input[tokenPlace];
        }

        private int GetInt()
        {
            int value = 0;
            while (true)
            {
                if (IsEndOfInput())
                    return value;

                char c = Input[tokenPlace];
                if (!char.IsDigit(c))
                    return value;

                value = (value * 10) + Input[tokenPlace] - CHAR_VALUE_ZERO;
                tokenPlace++;
            }
        }

        private bool IsEndOfInput()
        {
            if (tokenPlace >= Input.Length)
                return true;

            return false;
        }
    }
}
