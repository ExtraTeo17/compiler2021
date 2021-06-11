%using QUT.Gppg;
%namespace GardensPoint

IntNumber   [0-9]+
StringVar	\"(\\.|[^"\\])*\"

%%

"program"		{ return (int)Tokens.Program; }
"{"				{ return (int)Tokens.OpenBracket; }
"}"				{ return (int)Tokens.CloseBracket; }
"write"			{ return (int)Tokens.Write; }
{IntNumber}		{ yylval.val=yytext; return (int)Tokens.IntNumber; }
{StringVar}		{ yylval.val=yytext; return (int)Tokens.StringVar; }
" "				{ }
"\r"			{ }
"\t"			{ }
";"				{ return (int)Tokens.Semicolon; }
","				{ return (int)Tokens.Comma; }
"hex"			{ return (int)Tokens.Hex; }
<<EOF>>			{ return (int)Tokens.Eof; }
