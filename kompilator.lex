%using QUT.Gppg;
%namespace GardensPoint

IntNumber   [0-9]+
StringVar	\"(\\.|[^"\\])*\"
Boolean		true|false
RealNumber	[0-9]+\.[0-9]+
Ident		[A-Za-z][A-Za-z0-9]*

%%

"program"		{ return (int)Tokens.Program; }
"{"				{ return (int)Tokens.OpenBracket; }
"}"				{ return (int)Tokens.CloseBracket; }
"write"			{ return (int)Tokens.Write; }
"hex"			{ return (int)Tokens.Hex; }
"int"			{ yylval.val=yytext; return (int)Tokens.Int; }
"double"		{ yylval.val=yytext; return (int)Tokens.Double; }
"bool"			{ yylval.val=yytext; return (int)Tokens.Bool; }
{Boolean}		{ yylval.val=yytext; return (int)Tokens.Boolean; }
{IntNumber}		{ yylval.val=yytext; return (int)Tokens.IntNumber; }
{StringVar}		{ yylval.val=yytext; return (int)Tokens.StringVar; }
{RealNumber}	{ yylval.val=yytext; return (int)Tokens.RealNumber; }
{Ident}			{ yylval.val=yytext; return (int)Tokens.Ident; }
" "				{ }
"\r"			{ }
"\t"			{ }
";"				{ return (int)Tokens.Semicolon; }
","				{ return (int)Tokens.Comma; }
<<EOF>>			{ return (int)Tokens.Eof; }
