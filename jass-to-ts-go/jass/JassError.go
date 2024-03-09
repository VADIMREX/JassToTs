package jass

import (
	"fmt"
)

var isStrict = true

type JassError struct {
	message string
}

func (err *JassError) Error() string {
	return err.message
}

func GetIsStrict() bool {
	return isStrict
}
func SetIsStrict(value bool) {
	isStrict = value
}

func formatMessage(line int, col int, message string) string {
	return fmt.Sprintf("Line %d, Col %d: %v", line, col, message)
}

func NewJassError(line int, col int, message string) error {
	msg := formatMessage(line, col, message)
	if isStrict {
		return &JassError{message: msg}
	}
	fmt.Println(msg)
	return nil
}
