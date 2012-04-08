#include <string.h>
#include <stdio.h>

typedef struct {
  unsigned short registers[11];
  unsigned short memory[65536];
  unsigned char pages_accessed[65536 / 8 / 8];
} VMState;

extern void runMachine(VMState *state);

int main() {
  VMState state;
  memset(&state, 0, sizeof(state));
  runMachine(&state);

  return 0;
}

void output(unsigned short num) {
  printf("OUT: %d\n", num);
}

void debug(VMState *state) {
  unsigned int page;
  printf("DEBUG:\n");
  printf("  Register A: %d\n", state->registers[0]);
  printf("  Register B: %d\n", state->registers[1]);
  printf("  Register C: %d\n", state->registers[2]);
  printf("  Register X: %d\n", state->registers[3]);
  printf("  Register Y: %d\n", state->registers[4]);
  printf("  Register Z: %d\n", state->registers[5]);
  printf("  Register I: %d\n", state->registers[6]);
  printf("  Register J: %d\n", state->registers[7]);
  printf("  Register SP: %d\n", state->registers[8]);
  printf("  Register PC: %d\n", state->registers[9]);
  printf("  Register O: %d\n", state->registers[10]);
  printf("  Memory:\n");
  for (page = 0; page < 65536 / 8; page++) {
    if (state->pages_accessed[page / 8] & (1 << (page % 8))) {
      printf("    %04X: ", page);
      int word = 0;
      for (word = 0; word < 4; word++) {
        printf(" %04X", state->memory[page * 8 + word]);
      }
      printf(" ");
      for (word = 4; word < 8; word++) {
        printf(" %04X", state->memory[page * 8 + word]);
      }
      printf("\n");
    }
  }
}

void memory_referenced(VMState *state, unsigned short index) {
  unsigned short page = index / 8;
  state->pages_accessed[page / 8] |= 1 << (page % 8);
}
