
LETTERS_BY_STEP = [
    ["A", "B"],
    ["C", "D", "E"],
    ["F", "G", "H", "I"],
    ["J", "K", "L", "M", "N"],
    ["O", "P", "Q", "R", "S", "T"],
    ["U", "V", "W", "?", "X", "Y", "Z"],
]


def get_next_letters(letter):
    for step_index, letters in enumerate(LETTERS_BY_STEP[:-1]):
        if letter in letters:
            position = letters.index(letter)
            next_step = LETTERS_BY_STEP[step_index + 1]

            return [
                next_step[i]
                for i in (position, position + 1)
                if i < len(next_step)
            ]

    return []


LETTER_CONNECTIONS = {
    letter: get_next_letters(letter)
    for step in LETTERS_BY_STEP
    for letter in step
}
