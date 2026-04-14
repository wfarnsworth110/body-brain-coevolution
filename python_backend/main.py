from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List
import random
from deap import base, creator, tools

app = FastAPI(title="Body-Brain Co-evolution API")

""" DEAP setup """
# Fitness metric: Maximize distance traveled
creator.create("FitnessMax", base.Fitness, weights=(1.0,))
creator.create("Individual", list, fitness=creator.FitnessMax)

toolbox = base.Toolbox()
# 64 floats for morphology 64 for neural weights and biases = 128 total
toolbox.register("attr_float", random.uniform, 0.0, 1.0) # Easier to write to Unity as floats between 0 and 1, can be scaled in Unity
toolbox.register("individual", tools.initRepeat, creator.Individual, toolbox.attr_float, n=128)
toolbox.register("population", tools.initRepeat, list, toolbox.individual)

POPULATION_SIZE = 20
TOURNAMENT_SIZE = 3
CXPB = 0.7
MU = 0.0
SIGMA = 0.05
INDPB = 0.1

toolbox.register("select", tools.selTournament, tournsize=TOURNAMENT_SIZE)
toolbox.register("mate", tools.cxTwoPoint)
toolbox.register("mutate", tools.mutGaussian, mu=MU, sigma=SIGMA, indpb=INDPB)

population = toolbox.population(n=POPULATION_SIZE)
current_ind_index = 0

""" Generation Tracking """
current_generation = 1
max_generations = 100 # TODO This can be adjusted

""" API Models """
class FitnessReport(BaseModel):
    individual_id: int
    fitness_score: float
    generation: int # Unity sends this information too

class GenomeResponse(BaseModel):
    individual_id: int
    dna: List[float]
    generation: int
    is_finished: bool

""" Endpoints """
# Unity will call this to get the next genome to evaluate
@app.get("/get-genome", response_model=GenomeResponse)
async def get_genome():
    global current_ind_index, current_generation, population

    if current_generation > max_generations:
        return GenomeResponse(
            individual_id=-1,
            dna=[],
            generation=current_generation,
            is_finished=True
        )

    if current_ind_index >= len(population):
        if current_generation >= max_generations:
            current_generation += 1
            return GenomeResponse(
                individual_id=-1,
                dna=[],
                generation=current_generation,
                is_finished=True
            )

        offspring = toolbox.select(population, len(population))
        offspring = [toolbox.clone(ind) for ind in offspring]

        for i in range(1, len(offspring), 2):
            if random.random() < CXPB:
                offspring[i - 1], offspring[i] = toolbox.mate(offspring[i - 1], offspring[i])
                del offspring[i - 1].fitness.values
                del offspring[i].fitness.values

        for mutant in offspring:
            toolbox.mutate(mutant)
            del mutant.fitness.values

        for ind in offspring:
            for i in range(len(ind)):
                ind[i] = max(0.0, min(1.0, ind[i]))

        population[:] = offspring
        current_generation += 1
        current_ind_index = 0

    ind = population[current_ind_index]
    return GenomeResponse(
        individual_id=current_ind_index,
        dna=list(ind),
        generation=current_generation,
        is_finished=False
    )

@app.post("/post-fitness")
async def post_fitness(report: FitnessReport):
    global current_ind_index

    if report.generation != current_generation:
        raise HTTPException(
            status_code=400,
            detail=f"Generation mismatch: expected {current_generation}, got {report.generation}"
        )

    if report.individual_id < 0 or report.individual_id >= len(population):
        raise HTTPException(
            status_code=400,
            detail=f"individual_id {report.individual_id} out of range [0, {len(population) - 1}]"
        )

    # Assign the fitness to the DEAP individual
    population[report.individual_id].fitness.values = (report.fitness_score,)

    print(f"Gen {report.generation} | Ind {report.individual_id} scored: {report.fitness_score}")
    current_ind_index += 1

    return {"Status": "Success"}
