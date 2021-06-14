%using QUT.Gppg;
%namespace GardensPoint

IntNumber   0|[1-9][0-9]*
IntHexNum	0[x|X][A-Fa-f0-9]*
StringVar	\"(\\.|[^"\\\n])*\"
Comment		\/\/[^\n]*\n
BoolValue	true|false
RealNumber	(0|[1-9][0-9]*)\.[0-9]+
Ident		[A-Za-z][A-Za-z0-9]*

%%

"program"		{ return (int)Tokens.Program; }
"{"				{ return (int)Tokens.OpenBracket; }
"}"				{ return (int)Tokens.CloseBracket; }
"read"			{ return (int)Tokens.Read; }
"write"			{ return (int)Tokens.Write; }
"hex"			{ return (int)Tokens.Hex; }
"return"		{ return (int)Tokens.Return; }
"if"			{ return (int)Tokens.If; }
"else"			{ return (int)Tokens.Else; }
"while"			{ return (int)Tokens.While; }
"="				{ return (int)Tokens.Assign; }
"||"			{ return (int)Tokens.LogicalSum; }
"&&"			{ return (int)Tokens.LogicalProduct; }
"=="			{ return (int)Tokens.Equals; }
"!="			{ return (int)Tokens.NotEquals; }
">"				{ return (int)Tokens.GreaterThan; }
">="			{ return (int)Tokens.GreaterOrEqual; }
"<"				{ return (int)Tokens.LessThan; }
"<="			{ return (int)Tokens.LessOrEqual; }
"+"				{ return (int)Tokens.Plus; }
"-"				{ return (int)Tokens.Minus; }
"*"				{ return (int)Tokens.Multiplies; }
"/"				{ return (int)Tokens.Divides; }
"|"				{ return (int)Tokens.BitwiseSum; }
"&"				{ return (int)Tokens.BitwiseProduct; }
"~"				{ return (int)Tokens.BitwiseNegate; }
"!"				{ return (int)Tokens.LogicalNegate; }
"("				{ return (int)Tokens.OpenPar; }
")"				{ return (int)Tokens.ClosePar; }
"int"			{ yylval.val=yytext; return (int)Tokens.Int; }
"double"		{ yylval.val=yytext; return (int)Tokens.Double; }
"bool"			{ yylval.val=yytext; return (int)Tokens.Bool; }
{BoolValue}		{ yylval.val=yytext; return (int)Tokens.BoolValue; }
{IntNumber}		{ yylval.val=yytext; return (int)Tokens.IntNumber; }
{IntHexNum}		{ yylval.val=yytext; return (int)Tokens.IntHexNum; }
{StringVar}		{ yylval.val=yytext; return (int)Tokens.StringVar; }
{RealNumber}	{ yylval.val=yytext; return (int)Tokens.RealNumber; }
{Ident}			{ yylval.val=yytext; return (int)Tokens.Ident; }
{Comment}		{ }
" "				{ }
"\r"			{ Compiler.NextLine(); }
"\t"			{ }
";"				{ return (int)Tokens.Semicolon; }
","				{ return (int)Tokens.Comma; }
<<EOF>>			{ return (int)Tokens.Eof; }
.				{ Compiler.HandleLexicalError(yytext); }
