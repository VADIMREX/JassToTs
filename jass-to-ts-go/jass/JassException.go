package jass

import (
	"errors"
	"fmt"
)

var isStrict = true

func GetIsStrict() bool {
	return isStrict
}
func SetIsStrict(value bool) {
	isStrict = value
}

func formatMessage(line int, col int, message string) string {
	return fmt.Sprintf("Line %d, Col %d: %v", line, col, message)
}

func JassError(line int, col int, message string) error {
	msg := formatMessage(line, col, message)
	if isStrict {
		return errors.New(msg)
	}
	fmt.Println(msg)
	return nil
}
