package JassToTs

import "fmt"

type Statement struct {
	Type   string
	Start  *Token
	Parent *Statement
	Childs []*Statement
}

func newStatement() *Statement {
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
func (this *Statement) MakeChild(_type string, token *Token) *Statement {
	var child = &Statement{
		_type,
		token,
		this,
		[]*Statement{},
	}
	this.Childs = append(this.Childs, child)
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
func (this *Statement) AddChild(_type string, token *Token) *Statement {
	this.MakeChild(_type, token)
	return this
}

/**
* Добавить дочернюю инструкцию
*
* @param child дочерняя инструкция
* @return себя
 */
func (this *Statement) AddChildStatement(child *Statement) *Statement {
	child.Parent = this
	this.Childs = append(this.Childs, child)
	return this
}

func (this *Statement) String() string {
	return this.toString(0)
}

func (this *Statement) toString(ident int) string {
	var s = ""
	for _, statement := range this.Childs {
		s += statement.toString(ident + 2)
	}
	var sident = fmt.Sprintf("%d", ident);
	sident = fmt.Sprintf("%"+sident+"s", "");
	return fmt.Sprintf("%s%s %s\n%s", sident, this.Type, this.Start, s)
}