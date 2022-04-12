import React, { useState, useEffect } from "react";
import { Modal, Grid, Card, Text, Loader } from "@mantine/core";
import QuestionCard from "../Questions/QuestionCard";
import NewQuestion from "./NewQuestion";

function AllQuestionsPage() {
  const [openedNQ, setOpenedNQ] = useState(false);

  const [questions, setQuestions] = useState([]);

  useEffect(() => {
      let url = "./QuestionObject.json";
      fetch(url)
        .then((res) => res.json())
        .then((result) => {
          setQuestions(result);
        });
    }, []);

  return (
    <>
      <Modal
        opened={openedNQ}
        onClose={() => setOpenedNQ(false)}
        title="New Question"
        size="75%"
      >
        {<NewQuestion />}
      </Modal>

      <Grid>
        <Grid.Col md={6} lg={3}>
          <Card
            shadow="sm"
            p="lg"
            style={{ width: 340, margin: "auto", height: 307.23 }}
          >
            <Text
              align="center"
              text-align="center"
              variant="gradient"
              gradient={{ from: "indigo", to: "cyan", deg: 45 }}
              size="xl"
              weight={700}
              style={{
                fontFamily: "Greycliff CF, sans-serif",
                padding: "90px",
              }}
              onClick={() => setOpenedNQ(true)}
            >
              New Question <br /> +
            </Text>
          </Card>
        </Grid.Col>
        {questions.length !== 0 ? (
        questions.map((i: any) => {
          return (
            <Grid.Col md={6} lg={3} key={i.Id}>
              <QuestionCard
                Id={i.Id}
                Title={i.Title}
                Description={i.Description}
                Author={i.Author}
                Tags={i.Tags}
                Status={i.Status}
                Version={i.Version}
              />
            </Grid.Col>
          );
        })
      ) : (
        <>
          <Loader />
        </>
      )}
      </Grid>
    </>
  );
}

export default AllQuestionsPage;
