package jass

import "fmt"

type Statement struct {
	Type   string
	Start  *Token
	Parent *Statement
	Childs []*Statement
}

func NewStatement() *Statement {
	return &Statement{}
}

/**
* Создать и добавить дочернюю инструкцию
*
* @param type  тип инструкции
* @param token токен с которого начинается потомок
* @return дочернюю инструкцию
* @see Statement#AddChild
 */
func (s *Statement) MakeChild(_type string, token *Token) *Statement {
	var child = &Statement{
		_type,
		token,
		s,
		[]*Statement{},
	}
	s.Childs = append(s.Childs, child)
	return child
}

/**
* Создать и добавить дочернюю инструкцию
*
* @param type  тип инструкции
* @param token токен с которого начинается потомок
* @return себя
* @see Statement#MakeChild
 */
func (statement *Statement) AddChild(_type string, token *Token) *Statement {
	statement.MakeChild(_type, token)
	return statement
}

/**
* Добавить дочернюю инструкцию
*
* @param child дочерняя инструкция
* @return себя
 */
func (statement *Statement) AddChildStatement(child *Statement) *Statement {
	child.Parent = statement
	statement.Childs = append(statement.Childs, child)
	return statement
}

func (statement *Statement) String() string {
	return statement.toString(0)
}

func (statement *Statement) toString(ident int) string {
	var s = ""
	for _, statement := range statement.Childs {
		s += statement.toString(ident + 2)
	}
	var sident = fmt.Sprintf("%d", ident)
	sident = fmt.Sprintf("%"+sident+"s", "")
	return fmt.Sprintf("%s%s %s\n%s", sident, statement.Type, statement.Start, s)
}
