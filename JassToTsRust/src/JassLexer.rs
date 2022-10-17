use crate::Token;

pub struct JassLexer {
    isYdweCompatible: bool,
}
impl JassLexer {
    pub fn new(isYdweCompatible: bool) -> JassLexer {
        JassLexer {
            isYdweCompatible: isYdweCompatible,
        }
    }

    pub fn Tokenize(&mut self, source: &String) -> Vec<Token::Token> {
        let res: Vec<Token::Token> = vec![];
        return res;
    }
}