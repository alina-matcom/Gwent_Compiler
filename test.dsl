effect {
  Name: "Damage",
  Params: {
    Amount: Number
  },
  Action: (targets, context) => {
    for target in targets {
      i = 0;
      while (i++ < Amount)
        target.Power -= 1;
    }
  }
}